namespace Log.Application.Services.AuditSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using global::Log.Application.Interfaces;
using global::Log.Domain.Entities;
using global::Log.Domain.Enums;

/// <summary>Raises security alerts from newly stored audit records (A4). Three rules: BalanceTampering, PrivilegeEscalation, MassDelete.</summary>
public sealed class SecurityRuleEvaluator
{
    private readonly IApplicationDbContext _db;
    private readonly AuditSyncOptions _options;

    public SecurityRuleEvaluator(IApplicationDbContext db, IOptions<AuditSyncOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    /// <summary>Evaluates the given (already saved) records. Returns the number of alerts created.</summary>
    public async Task<int> EvaluateAsync(IReadOnlyList<CentralAuditLog> records, CancellationToken ct)
    {
        var created = 0;
        foreach (var record in records.OrderBy(r => r.CreatedAt))
        {
            var rule = await MatchAsync(record, ct);
            if (rule is null) continue;

            var (type, severity, title, description) = rule.Value;
            var exists = await _db.SecurityAuditAlerts.AnyAsync(a => a.CentralAuditLogId == record.Id && a.TriggeredRule == type, ct);
            if (exists) continue;

            _db.SecurityAuditAlerts.Add(new SecurityAuditAlert
            {
                Id = Guid.NewGuid(),
                AlertTitle = title,
                Severity = severity,
                ServiceName = record.ServiceName,
                CentralAuditLogId = record.Id,
                TriggeredRule = type,
                Description = description,
                Status = AlertStatus.Open
            });
            created++;
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return created;
    }

    private async Task<(SecurityRuleType, AlertSeverity, string, string)?> MatchAsync(CentralAuditLog record, CancellationToken ct)
    {
        switch (record.ActionName)
        {
            case "RoleAssign":
                return (SecurityRuleType.PrivilegeEscalation, AlertSeverity.High, "Role assignment changed",
                    $"Roles of {record.EntityName} {record.EntityId} were changed by {record.UserId ?? "unknown"}.");

            case "BalanceAdjustment":
            {
                var amount = ReadAmount(record.NewValues);
                if (amount >= _options.BalanceAdjustmentThreshold)
                {
                    return (SecurityRuleType.BalanceTampering, AlertSeverity.Critical, "Large manual balance adjustment",
                        $"Wallet {record.EntityId} was adjusted by {amount:0.##} (threshold {_options.BalanceAdjustmentThreshold}).");
                }

                var since = record.CreatedAt.AddMinutes(-_options.BalanceAdjustmentWindowMinutes);
                var count = await _db.CentralAuditLogs.CountAsync(
                    l => l.ActionName == "BalanceAdjustment" && l.UserId == record.UserId && l.CreatedAt >= since && l.CreatedAt <= record.CreatedAt, ct);
                if (count >= _options.BalanceAdjustmentCount)
                {
                    return (SecurityRuleType.BalanceTampering, AlertSeverity.High, "Repeated manual balance adjustments",
                        $"{count} manual balance adjustments by {record.UserId ?? "unknown"} within {_options.BalanceAdjustmentWindowMinutes} minutes.");
                }

                return null;
            }

            case "SoftDelete":
            case "Delete":
            {
                var since = record.CreatedAt.AddMinutes(-_options.MassDeleteWindowMinutes);
                var count = await _db.CentralAuditLogs.CountAsync(
                    l => (l.ActionName == "SoftDelete" || l.ActionName == "Delete") && l.UserId == record.UserId && l.CreatedAt >= since && l.CreatedAt <= record.CreatedAt, ct);
                // Alert once when the threshold is crossed, not on every further delete.
                if (count == _options.MassDeleteCount)
                {
                    return (SecurityRuleType.MassDelete, AlertSeverity.High, "Many deletes in a short time",
                        $"{count} deletes by {record.UserId ?? "unknown"} within {_options.MassDeleteWindowMinutes} minutes.");
                }

                return null;
            }

            default:
                return null;
        }
    }

    private static decimal ReadAmount(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0m;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return 0m;
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("Amount") || string.Equals(property.Name, "amount", StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.Number ? Math.Abs(property.Value.GetDecimal()) : 0m;
                }
            }
        }
        catch (JsonException)
        {
        }

        return 0m;
    }
}

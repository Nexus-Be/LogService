namespace Log.Infrastructure.Services.AuditSync;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Log.Application.Interfaces;
using global::Log.Application.Services.AuditSync;

/// <summary>Runs the audit sync on a timer when AuditSync:Enabled is true. Refuses to start without the redaction filter (A8 gate).</summary>
public sealed class AuditSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly AuditSyncOptions _options;
    private readonly ILogger<AuditSyncWorker> _logger;

    public AuditSyncWorker(IServiceScopeFactory scopes, IOptions<AuditSyncOptions> options, ILogger<AuditSyncWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Audit sync is disabled (AuditSync:Enabled = false).");
            return;
        }

        using (var gate = _scopes.CreateScope())
        {
            if (gate.ServiceProvider.GetService<IAuditRedactor>() is null)
            {
                _logger.LogError("Audit sync is enabled but the redaction filter is not registered. The worker will not run.");
                return;
            }
        }

        _logger.LogInformation("Audit sync started: every {Seconds}s, batch {Batch}.", _options.IntervalSeconds, _options.BatchSize);
        var lastCleanup = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<AuditSyncRunner>();
                var stored = await runner.RunOnceAsync(stoppingToken);
                if (stored > 0) _logger.LogInformation("Audit sync stored {Count} records.", stored);

                if (DateTimeOffset.UtcNow - lastCleanup > TimeSpan.FromHours(6))
                {
                    var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays);
                    var old = await db.CentralAuditLogs.Where(c => c.CreatedAt < cutoff && !c.SecurityAlerts.Any()).Take(1000).ToListAsync(stoppingToken);
                    if (old.Count > 0)
                    {
                        db.CentralAuditLogs.RemoveRange(old);
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    lastCleanup = DateTimeOffset.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit sync pass failed; it will be tried again.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds)), stoppingToken);
        }
    }
}

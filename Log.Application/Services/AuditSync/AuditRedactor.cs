namespace Log.Application.Services.AuditSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>Hides sensitive values in audit payloads before they are stored centrally (A8).</summary>
public interface IAuditRedactor
{
    /// <summary>Returns valid JSON with sensitive properties replaced by the redaction marker, or null for empty input.</summary>
    string? Redact(string? payload);
}

public sealed class AuditRedactor : IAuditRedactor
{
    public const string Marker = "[REDACTED]";

    // Same names the wallet service hides when it shows withdrawal history, plus credentials and secrets.
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "BankName", "BankCode", "BankAccountNumber", "AccountNumber", "BankAccountHolder", "AccountHolderName",
        "BankAccountId", "VerificationEvidence",
        "PasswordHash", "Password", "NewPassword", "OldPassword", "ConfirmPassword",
        "Token", "AccessToken", "RefreshToken", "IdToken", "Secret", "ClientSecret", "ApiKey", "PrivateKey",
        "Otp", "OtpCode", "CardNumber", "Cvv", "Authorization", "PrintToken"
    };

    public string? Redact(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(payload);
        }
        catch (JsonException)
        {
            // Not JSON (a plain note). Keep it as a JSON string so it still fits the jsonb column.
            return JsonSerializer.Serialize(payload);
        }

        if (node is null)
        {
            return null;
        }

        Walk(node);
        return node.ToJsonString();
    }

    private static void Walk(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kv => kv.Key).ToList())
                {
                    if (SensitiveNames.Contains(key))
                    {
                        obj[key] = Marker;
                    }
                    else if (obj[key] is JsonNode child)
                    {
                        Walk(child);
                    }
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null) Walk(item);
                }
                break;
        }
    }
}

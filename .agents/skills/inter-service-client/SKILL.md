---
name: inter-service-client
description: Add or modify outbound typed REST clients in LogService (port 8004). Use when LogService dispatches high-severity security or error alerts to NotificationService without using message brokers.
---

# Outbound REST Client in LogService

> Scope: LogService repository only. Never reference parent workspace paths, root documentation, or sibling service directories.

## Local Facts

- Service: LogService
- Local HTTP Port: 8004
- Primary Downstream Target: NotificationService (`8007`) for security incident email dispatch
- Interface Location: `Log.Application/Interfaces/`
- Implementation Location: `Log.Infrastructure/Services/{Target}/`
- DI Registration: `Log.Infrastructure/DependencyInjection.cs`

## Invariants

1. **100% RESTful HTTP/JSON:** No RabbitMQ, MassTransit, Saga, or Outbox. All cross-service calls use typed `HttpClient`.
2. **Strict Project Isolation:** Never reference external assemblies or query other microservice databases directly.
3. **No Internal API Key:** Inter-service routes use standard public endpoints. Do not send `X-Internal-Service-Key`.
4. **Append-Only Isolation:** Sending outbound alerts must never interfere with the local `Logs` append-only database transaction.

## Implementation Steps

### 1. Application Contract
Define `Log.Application/Interfaces/IAlertNotificationClient.cs`:
```csharp
namespace Log.Application.Interfaces;

public interface IAlertNotificationClient
{
    Task<bool> SendSecurityAlertEmailAsync(
        string recipientEmail,
        string alertTitle,
        string alertDetails,
        CancellationToken cancellationToken = default);
}
```

### 2. Infrastructure Typed Client
Implement `Log.Infrastructure/Services/Notification/AlertNotificationClient.cs`:
```csharp
namespace Log.Infrastructure.Services.Notification;

public sealed class AlertNotificationClient : IAlertNotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlertNotificationClient> _logger;

    public AlertNotificationClient(HttpClient httpClient, ILogger<AlertNotificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendSecurityAlertEmailAsync(
        string recipientEmail,
        string alertTitle,
        string alertDetails,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                toEmail = recipientEmail,
                subject = $"[SECURITY ALERT] {alertTitle}",
                htmlBody = $"<pre>{alertDetails}</pre>"
            };

            var response = await _httpClient.PostAsJsonAsync("api/v1/notifications/email/send", payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[REST Alert Client] Failed to dispatch security alert to NotificationService.");
            return false;
        }
    }
}
```

### 3. Dependency Injection Registration
In `Log.Infrastructure/DependencyInjection.cs`:
```csharp
var notificationBaseUrl = Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL")
    ?? configuration["NOTIFICATION_SERVICE_URL"]
    ?? configuration["Services:Notification:BaseUrl"]
    ?? "http://localhost:8007/";

services.AddHttpClient<IAlertNotificationClient, AlertNotificationClient>(client =>
{
    client.BaseAddress = new Uri(notificationBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

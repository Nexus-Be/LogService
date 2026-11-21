namespace Log.Infrastructure.Services.AuditSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using global::Log.Application.Services.AuditSync;

/// <summary>Reads audit records from the source services' internal routes, with the shared internal service key.</summary>
public sealed class HttpAuditSourceClient : IAuditSourceClient, IHealthProbe
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _factory;
    private readonly IServiceProvider _scopes;
    private readonly string _key;
    private readonly Dictionary<string, string> _urls;

    public HttpAuditSourceClient(IHttpClientFactory factory, IServiceProvider scopes, IConfiguration configuration)
    {
        _factory = factory;
        _scopes = scopes;
        _key = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_SECRET")
            ?? configuration["INTERNAL_SERVICE_SECRET"]
            ?? configuration["InternalSecurity:ServiceKey"]
            ?? string.Empty;

        string Url(string name, string env, string fallback) =>
            (configuration[$"AuditSync:Sources:{name}"] ?? configuration[env] ?? fallback).TrimEnd('/');

        _urls = new Dictionary<string, string>
        {
            ["user-service"] = Url("user-service", "USER_SERVICE_URL", "http://user-service:8001"),
            ["catalog-service"] = Url("catalog-service", "CATALOG_SERVICE_URL", "http://catalog-service:8002"),
            ["commerce-service"] = Url("commerce-service", "COMMERCE_SERVICE_URL", "http://commerce-service:8003"),
            ["auction-service"] = Url("auction-service", "AUCTION_SERVICE_URL", "http://auction-service:8005"),
            ["fulfillment-service"] = Url("fulfillment-service", "FULFILLMENT_SERVICE_URL", "http://fulfillment-service:8006"),
            ["notification-service"] = Url("notification-service", "NOTIFICATION_SERVICE_URL", "http://notification-service:8007"),
            ["wallet-service"] = Url("wallet-service", "WALLET_SERVICE_URL", "http://wallet-service:8008"),
        };
    }

    public IReadOnlyList<string> SourceNames => _urls.Keys.ToList();

    IReadOnlyList<string> IHealthProbe.ServiceNames => _urls.Keys.Append("log-service").ToList();

    public async Task<SourceAuditPage> GetUnsyncedAsync(string source, int limit, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_urls[source]}/api/v1/internal/audit-logs/unsynced?limit={limit}");
        request.Headers.Add("X-Internal-Service-Key", _key);
        var client = _factory.CreateClient("audit-source");
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{source} answered {(int)response.StatusCode}.");
        }

        var page = await response.Content.ReadFromJsonAsync<SourceAuditPage>(Json, ct)
            ?? throw new HttpRequestException($"{source} returned an empty body.");
        return page;
    }

    public async Task MarkSyncedAsync(string source, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_urls[source]}/api/v1/internal/audit-logs/mark-synced")
        {
            Content = JsonContent.Create(new { ids }, options: Json)
        };
        request.Headers.Add("X-Internal-Service-Key", _key);
        var client = _factory.CreateClient("audit-source");
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{source} refused mark-synced with {(int)response.StatusCode}.");
        }
    }

    public async Task<ServiceHealthResponse> CheckAsync(string service, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;
        if (service == "log-service")
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<global::Log.Infrastructure.Persistence.LogDbContext>();
                var ok = await db.Database.CanConnectAsync(ct);
                var ms = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                return new ServiceHealthResponse(service, ok ? "Healthy" : "Unhealthy", ms, started, ok ? null : "The database is not reachable.");
            }
            catch (Exception ex)
            {
                return new ServiceHealthResponse(service, "Unhealthy", 0, started, ex.Message);
            }
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            var client = _factory.CreateClient("audit-source");
            // Notification answers /health with "the process is up" only. Its internal route lists the database and every worker.
            if (service == "notification-service")
            {
                return await CheckNotificationAsync(started, timeout.Token);
            }

            using var response = await client.GetAsync($"{_urls[service]}/health", timeout.Token);
            var latency = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new ServiceHealthResponse(service, "Unhealthy", latency, started, $"Health check answered {(int)response.StatusCode}.");
            }

            var degraded = body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);
            return new ServiceHealthResponse(service, degraded ? "Degraded" : "Healthy", latency, started, null);
        }
        catch (Exception ex)
        {
            var latency = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
            return new ServiceHealthResponse(service, "Unhealthy", latency, started, ex is OperationCanceledException ? "The service did not answer in time." : ex.Message);
        }
    }

    private static bool Find(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) { value = property.Value; return true; }
            }
        }

        value = default;
        return false;
    }

    private async Task<ServiceHealthResponse> CheckNotificationAsync(DateTimeOffset started, CancellationToken ct)
    {
        const string service = "notification-service";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_urls[service]}/api/v1/internal/health/workers");
        request.Headers.Add("X-Internal-Service-Key", _key);
        var client = _factory.CreateClient("audit-source");
        using var response = await client.SendAsync(request, ct);
        var latency = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
        if (!response.IsSuccessStatusCode)
        {
            return new ServiceHealthResponse(service, "Unhealthy", latency, started, $"Health check answered {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        var workers = new List<WorkerHealthResponse>();
        if (Find(root, "database", out var database) && Find(database, "status", out var dbStatus))
        {
            workers.Add(new WorkerHealthResponse("database", dbStatus.GetString() ?? "Unknown"));
        }

        if (Find(root, "workers", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var worker in list.EnumerateArray())
            {
                var name = Find(worker, "name", out var n) ? n.GetString() ?? "worker" : "worker";
                var status = Find(worker, "status", out var s) ? s.GetString() ?? "Unknown" : "Unknown";
                workers.Add(new WorkerHealthResponse(name, status));
            }
        }

        var overall = Find(root, "status", out var st) ? st.GetString() : null;
        var healthy = string.Equals(overall, "Healthy", StringComparison.OrdinalIgnoreCase);
        var broken = workers.Where(w => w.Status is not ("Running" or "Waiting" or "Disabled" or "Healthy")).Select(w => w.Name).ToList();
        return new ServiceHealthResponse(service, healthy ? "Healthy" : "Unhealthy", latency, started,
            healthy ? null : $"Not ready: {(broken.Count > 0 ? string.Join(", ", broken) : "see workers")}.", workers);
    }
}

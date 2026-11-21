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
            // Notification answers /health with "the process is up" only; its /health/ready also checks the database and its workers.
            var path = service == "notification-service" ? "/health/ready" : "/health";
            using var response = await client.GetAsync($"{_urls[service]}{path}", timeout.Token);
            var latency = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            IReadOnlyList<WorkerHealthResponse>? workers = null;
            string? detail = null;
            if (service == "notification-service")
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    string Read(string name) => root.TryGetProperty(name, out var v) ? v.GetString() ?? string.Empty : string.Empty;
                    workers = new[] { new WorkerHealthResponse("database", Read("database")), new WorkerHealthResponse("workers", Read("workers")) };
                    if (!response.IsSuccessStatusCode) detail = $"Database {Read("database")}, workers {Read("workers")}.";
                }
                catch (JsonException)
                {
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ServiceHealthResponse(service, "Unhealthy", latency, started, detail ?? $"Health check answered {(int)response.StatusCode}.", workers);
            }

            var degraded = body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);
            return new ServiceHealthResponse(service, degraded ? "Degraded" : "Healthy", latency, started, null, workers);
        }
        catch (Exception ex)
        {
            var latency = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
            return new ServiceHealthResponse(service, "Unhealthy", latency, started, ex is OperationCanceledException ? "The service did not answer in time." : ex.Message);
        }
    }
}

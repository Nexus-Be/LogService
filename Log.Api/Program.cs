using Log.Api.Common;
using Log.Api.Extensions;
using Log.Application;
using Log.Infrastructure;
using Log.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Auxiliary Service Defaults (Observability, Swagger, JSON options)
builder.Services.AddServiceDefaults(builder.Configuration, "log-service");

// 2. Application & Infrastructure Dependency Injection
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. REST Services
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiValidationProblemFactory.Create;
    });

var app = builder.Build();

// 4. Default Middleware Pipeline (Exception Handler, Swagger UI)
app.UseServiceDefaults();

// Automatically apply database migrations on startup if relational DB is available
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
    try
    {
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Database migration deferred on startup: {Message}", ex.Message);
    }
}

app.MapControllers();
app.MapGet("/health", async (LogDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "Healthy", service = "log-service" })
            : Results.Json(new { status = "Unhealthy", service = "log-service", message = "The database is not reachable." }, statusCode: 503);
    }
    catch (Exception)
    {
        return Results.Json(new { status = "Unhealthy", service = "log-service", message = "The database check failed." }, statusCode: 503);
    }
});

app.Run();

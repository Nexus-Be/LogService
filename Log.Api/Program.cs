using Log.Api.Extensions;
using Log.Api.Services;
using Log.Application;
using Log.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. OpenTelemetry Distributed Tracing
builder.Services.AddServiceObservability(builder.Configuration, "Log.Api");

// 2. Application & Infrastructure Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. gRPC Server
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// 4. Swagger / OpenAPI for REST endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 5. Map gRPC Services
app.MapGrpcService<LogGrpcService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. LogService is running.");

app.Run();

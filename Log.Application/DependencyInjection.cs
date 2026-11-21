namespace Log.Application;

using Microsoft.Extensions.DependencyInjection;
using global::Log.Application.Services.Log;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ILogService, LogService>();
        services.AddSingleton<global::Log.Application.Services.AuditSync.AuditSyncStateStore>();
        services.AddSingleton<global::Log.Application.Services.AuditSync.IAuditRedactor, global::Log.Application.Services.AuditSync.AuditRedactor>();
        services.AddScoped<global::Log.Application.Services.AuditSync.SecurityRuleEvaluator>();
        services.AddScoped<global::Log.Application.Services.AuditSync.AuditSyncRunner>();
        services.AddScoped<global::Log.Application.Services.AuditSync.AdminAuditService>();
        return services;
    }
}

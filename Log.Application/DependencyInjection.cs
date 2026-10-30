namespace Log.Application;

using Microsoft.Extensions.DependencyInjection;
using global::Log.Application.Services.Log;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ILogService, LogService>();
        return services;
    }
}

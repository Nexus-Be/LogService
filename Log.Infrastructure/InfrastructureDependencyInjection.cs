namespace Log.Infrastructure;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using global::Log.Application.Common.Interfaces;
using global::Log.Application.Consumers;
using global::Log.Infrastructure.Persistence;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5436;Database=log_db;Username=postgres;Password=MatkhaucuaTui123@";

        services.AddDbContext<LogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(LogDbContext).Assembly.FullName);
                b.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<LogDbContext>());

        // MassTransit + RabbitMQ + Transactional Outbox
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<GlobalLogConsumer>();

            x.AddEntityFrameworkOutbox<LogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMq:Host"] ?? "localhost";
                var rabbitPort = ushort.TryParse(configuration["RabbitMq:Port"], out var port) ? port : (ushort)5672;
                var rabbitUser = configuration["RabbitMq:User"] ?? "guest";
                var rabbitPass = configuration["RabbitMq:Password"] ?? "MatkhaucuaTui123@";

                cfg.Host(rabbitHost, rabbitPort, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

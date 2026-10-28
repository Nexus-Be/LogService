namespace Log.Application.Consumers;

using MassTransit;
using Log.Application.Consumers.SystemEvents;

public static class ConsumerExtensions
{
    public static IBusRegistrationConfigurator AddApplicationConsumers(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GlobalLogConsumer>();
        return configurator;
    }
}

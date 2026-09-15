using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Contracts.Tasks;
using FinBeat.TaskManagement.Infrastructure.Messaging;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FinBeat.TaskManagement.Infrastructure;

/// <summary>Wires the persistence and messaging adapters into a service collection.</summary>
public static class DependencyInjection
{
    /// <summary>The configuration section bound to <see cref="RabbitMqTransportOptions"/>.</summary>
    public const string RabbitMqSectionName = "RabbitMq";

    /// <summary>The exchange and queue that collect events no consumer queue was bound to receive.</summary>
    public const string UnroutableName = "unroutable";

    /// <summary>The events published by this service, which is every contract the wire format declares.</summary>
    public static IReadOnlyList<Type> IntegrationEventTypes { get; } = typeof(TaskCreated).Assembly.GetExportedTypes();

    /// <summary>Registers the database, the message bus, and the ports the application layer declares.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration the connection settings are read from.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.TaskManagement));

        // Resolves the context AddDbContext registered rather than constructing a second one.
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddMessaging(configuration);

        return services;
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        // MassTransit binds these itself and defaults to localhost:5672 guest/guest, so no Host call.
        services.AddOptions<RabbitMqTransportOptions>().Bind(configuration.GetSection(RabbitMqSectionName));

        services.AddMassTransit(bus =>
        {
            // A publish and the state change that caused it commit together, or neither does.
            bus.AddEntityFrameworkOutbox<ApplicationDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.SetKebabCaseEndpointNameFormatter();

            bus.UsingRabbitMq((context, rabbit) =>
            {
                // Load-bearing. Every publish here goes through the outbox, and the outbox send path
                // declares only the exchange it sends to - argument and all, but not the alternate
                // exchange that argument names. Without the alternate exchange on the broker before
                // the first publish, RabbitMQ drops the diverted message exactly as it would have
                // dropped the original.
                rabbit.DeployPublishTopology = true;

                // The outbox guarantees the broker accepted the event, not that anyone was listening.
                // A fanout exchange with nothing bound discards silently - no error, no log, nothing
                // returned to the publisher - so an event published before the listener first declares
                // its queue simply disappears. The alternate exchange catches those instead, turning
                // that silence into a queue you can watch filling up.
                //
                // Driven off the assembly rather than a list, so a new contract cannot be added without it.
                foreach (var integrationEvent in IntegrationEventTypes)
                {
                    rabbit.Publish(integrationEvent, exchange => exchange.BindAlternateExchangeQueue(UnroutableName));
                }

                rabbit.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}

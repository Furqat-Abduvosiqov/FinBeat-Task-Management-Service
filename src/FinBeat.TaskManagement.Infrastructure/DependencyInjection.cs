using FinBeat.TaskManagement.Application.Abstractions;
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
            bus.UsingRabbitMq((context, rabbit) => rabbit.ConfigureEndpoints(context));
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}

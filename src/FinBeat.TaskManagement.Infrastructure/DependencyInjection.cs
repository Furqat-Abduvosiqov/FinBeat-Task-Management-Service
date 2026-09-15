using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Infrastructure.Messaging;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBeat.TaskManagement.Infrastructure;

/// <summary>Wires the persistence and messaging adapters into a service collection.</summary>
public static class DependencyInjection
{
    /// <summary>The connection string name, read from <c>ConnectionStrings:TaskManagement</c>.</summary>
    public const string ConnectionStringName = "TaskManagement";

    /// <summary>The configuration section holding the RabbitMQ connection settings.</summary>
    public const string RabbitMqSectionName = "RabbitMq";

    /// <summary>Registers the database, the message bus, and the ports the application layer declares.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration the connection settings are read from.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No connection string is configured under <see cref="ConnectionStringName"/>.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        // Fail at startup with the missing key named, rather than a null reference from inside Npgsql
        // on whichever request happens to run the first query.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string configured for '{ConnectionStringName}'. Set "
                + $"'ConnectionStrings:{ConnectionStringName}' in configuration, or the "
                + $"'ConnectionStrings__{ConnectionStringName}' environment variable.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        // Resolves the context AddDbContext registered rather than constructing a second one.
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddMessaging(configuration);

        return services;
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            // The outbox writes a publish into ApplicationDbContext inside the caller's transaction and
            // delivers it afterwards, so a task is never saved without its event and an event is never
            // sent for a task that rolled back.
            bus.AddEntityFrameworkOutbox<ApplicationDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.SetKebabCaseEndpointNameFormatter();

            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = configuration.GetSection(RabbitMqSectionName);

                rabbit.Host(
                    options["Host"] ?? "localhost",
                    options["VirtualHost"] ?? "/",
                    host =>
                    {
                        host.Username(options["Username"] ?? "guest");
                        host.Password(options["Password"] ?? "guest");
                    });

                rabbit.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}

using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBeat.TaskManagement.Infrastructure;

/// <summary>Wires the persistence layer into a service collection.</summary>
public static class DependencyInjection
{
    /// <summary>The connection string name, read from <c>ConnectionStrings:TaskManagement</c>.</summary>
    public const string ConnectionStringName = "TaskManagement";

    /// <summary>Registers <see cref="ApplicationDbContext"/> and <see cref="IApplicationDbContext"/>.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration the connection string is read from.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No connection string is configured under <see cref="ConnectionStringName"/>.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

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

        return services;
    }
}

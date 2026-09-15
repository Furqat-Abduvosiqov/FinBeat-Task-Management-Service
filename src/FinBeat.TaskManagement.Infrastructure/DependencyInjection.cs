using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBeat.TaskManagement.Infrastructure;

/// <summary>Wires the persistence layer into a service collection.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// The connection string name <see cref="AddInfrastructure"/> reads: configuration key
    /// <c>ConnectionStrings:TaskManagement</c>, environment variable <c>ConnectionStrings__TaskManagement</c>.
    /// </summary>
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

        // Fail at startup with the missing key named, rather than a null reference from inside Npgsql
        // on whichever request happens to run the first query.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string configured for '{ConnectionStringName}'. Set "
                + $"'ConnectionStrings:{ConnectionStringName}' in configuration, or the "
                + $"'ConnectionStrings__{ConnectionStringName}' environment variable.");
        }

        // The provider and the connection string are the whole configuration. Snake-case naming is
        // applied by the context itself, in OnConfiguring, so every path that builds a context gets an
        // identical model whether or not it came through this method.
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        // Resolves the scoped context AddDbContext already registered, rather than constructing a
        // second one. AddScoped<IApplicationDbContext, ApplicationDbContext>() would give one request
        // two contexts: a use case would track its changes on one and call SaveChangesAsync on the
        // other, which saves nothing and reports success.
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}

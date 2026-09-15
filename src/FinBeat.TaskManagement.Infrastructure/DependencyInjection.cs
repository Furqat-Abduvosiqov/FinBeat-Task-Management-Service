using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FinBeat.TaskManagement.Infrastructure;

/// <summary>Wires the persistence layer into a service collection.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// The connection string name <see cref="AddInfrastructure"/> reads: configuration key
    /// <c>ConnectionStrings:TaskManagement</c>, environment variable <c>ConnectionStrings__TaskManagement</c>.
    /// </summary>
    public const string ConnectionStringName = "TaskManagement";

    /// <summary>Registers <see cref="ApplicationDbContext"/>, its <see cref="NpgsqlDataSource"/>, and <see cref="IApplicationDbContext"/>.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration the connection string is read from.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// No connection string is configured under <see cref="ConnectionStringName"/>.
    /// </exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        // Fail at startup with the missing key named, rather than a null reference exception from
        // inside Npgsql on whichever request happens to run the first query.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string configured for '{ConnectionStringName}'. Set "
                + $"'ConnectionStrings:{ConnectionStringName}' in configuration, or the "
                + $"'ConnectionStrings__{ConnectionStringName}' environment variable.");
        }

        // A data source owns the connection pool and the cached type catalogue (see
        // ApplicationDbContextOptions.CreateDataSource). Registered here, rather than built inline
        // inside the AddDbContext delegate below, so the container - not each context - owns its
        // disposal.
        services.AddSingleton(sp =>
            ApplicationDbContextOptions.CreateDataSource(connectionString, sp.GetService<ILoggerFactory>()));

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            ApplicationDbContextOptions.Configure(options, sp.GetRequiredService<NpgsqlDataSource>()));

        // Resolves the scoped context AddDbContext already registered, rather than constructing a
        // second one. AddScoped<IApplicationDbContext, ApplicationDbContext>() would give one request
        // two contexts: a use case would track changes on one and call SaveChangesAsync on the other,
        // which saves nothing and reports success.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Deliberately not enabled:
        //  - EnableRetryOnFailure: its execution strategy makes any user-initiated transaction throw
        //    unless every use is wrapped in strategy.ExecuteAsync, and nothing here does that yet.
        //  - EnableSensitiveDataLogging: parameter values would end up in logs; that is a
        //    data-handling decision to make deliberately, not a default to opt into silently.
        //  - UseXminAsConcurrencyToken: changes every UPDATE to add "WHERE xmin = @p" and starts
        //    throwing DbUpdateConcurrencyException that nothing in this codebase catches yet.
        return services;
    }
}

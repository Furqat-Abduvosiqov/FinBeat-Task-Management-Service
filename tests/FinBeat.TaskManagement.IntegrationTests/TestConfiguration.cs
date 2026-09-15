using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace FinBeat.TaskManagement.IntegrationTests;

/// <summary>The configuration tests hand to <see cref="DependencyInjection.AddInfrastructure"/>, in the shape a host supplies it.</summary>
internal static class TestConfiguration
{
    /// <summary>The key the connection string is read from.</summary>
    public const string ConnectionStringKey =
        $"{DatabaseOptions.SectionName}:{DatabaseOptions.ConnectionStringName}";

    /// <summary>A connection string that parses but is never dialled, for tests that reach no database.</summary>
    public const string UnusedConnectionString =
        "Host=localhost;Port=5432;Database=never_connected;Username=none;Password=none";

    /// <summary>Configuration carrying one connection string and nothing else.</summary>
    /// <param name="connectionString">The connection string to supply, or null to supply none.</param>
    public static IConfiguration ForDatabase(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConnectionStringKey] = connectionString })
            .Build();
}

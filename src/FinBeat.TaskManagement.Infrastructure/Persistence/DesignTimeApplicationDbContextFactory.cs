using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>Creates an <see cref="ApplicationDbContext"/> for design-time tooling: <c>dotnet ef</c>.</summary>
/// <remarks>
/// <para>
/// Without this factory, the tooling has to fall back to booting the application host to find a
/// context, which makes generating DDL depend on the entire composition root being wired up — a
/// dependency a schema tool should not have.
/// </para>
/// <para>
/// <c>migrations add</c> and <c>migrations script</c> never open a connection, so the connection
/// string only has to parse. <c>database update</c> does open one, which is what the environment
/// variable is for: it lets a developer point this factory at a real database without editing source.
/// </para>
/// </remarks>
public sealed class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // A schema tool needs something that parses even when nobody has configured anything — migrations
    // add and migrations script never open a connection, so this default is never asked to point at a
    // reachable database.
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=finbeat_taskmanagement;Username=postgres;Password=postgres";

    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Read the connection string the way the host reads it, rather than reaching for a named
        // environment variable directly. The environment-variable provider is what translates
        // ConnectionStrings__TaskManagement into the ConnectionStrings:TaskManagement key that
        // GetConnectionString looks up, so the double-underscore spelling lives in the framework
        // instead of being restated here.
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(DependencyInjection.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = FallbackConnectionString;
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

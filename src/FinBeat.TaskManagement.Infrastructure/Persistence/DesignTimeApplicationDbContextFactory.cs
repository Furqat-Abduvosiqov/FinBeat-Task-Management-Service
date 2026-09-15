using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>Creates an <see cref="ApplicationDbContext"/> for design-time tooling: <c>dotnet ef</c>.</summary>
/// <remarks>
/// <para>
/// Without this factory, the tooling has to fall back to booting the application's host to find a
/// context, which makes generating DDL depend on the entire composition root being wired up — a
/// dependency a schema tool should not have.
/// </para>
/// <para>
/// <c>migrations add</c> and <c>migrations script</c> never open a connection, so the connection
/// string only has to parse. <c>database update</c> does open one, which is what
/// <see cref="DependencyInjection.ConnectionStringName"/> is for: it lets a developer
/// point this factory at a real database without editing source.
/// </para>
/// <para>This factory runs only under the tooling. Nothing here is read at runtime.</para>
/// </remarks>
public sealed class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // A schema tool needs something that parses even when nobody has set the environment variable —
    // migrations add and migrations script never open a connection, so this default is never asked
    // to actually point at a reachable database.
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=finbeat_taskmanagement;Username=postgres;Password=postgres";

    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(DependencyInjection.ConnectionStringName)
            ?? FallbackConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        ApplicationDbContextOptions.ConfigureFromConnectionString(optionsBuilder, connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

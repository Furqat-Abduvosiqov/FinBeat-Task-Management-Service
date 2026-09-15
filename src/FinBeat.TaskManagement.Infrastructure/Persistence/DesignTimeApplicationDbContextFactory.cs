using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>Creates an <see cref="ApplicationDbContext"/> for <c>dotnet ef</c>.</summary>
/// <remarks>
/// Lets the tooling generate DDL without booting the application host. Set
/// <c>ConnectionStrings__TaskManagement</c> to point <c>database update</c> at a real database;
/// <c>migrations add</c> and <c>migrations script</c> never open a connection.
/// </remarks>
public sealed class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=finbeat_taskmanagement;Username=postgres;Password=postgres";

    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Binds ConnectionStrings__TaskManagement the same way the host does.
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = FallbackConnectionString;
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

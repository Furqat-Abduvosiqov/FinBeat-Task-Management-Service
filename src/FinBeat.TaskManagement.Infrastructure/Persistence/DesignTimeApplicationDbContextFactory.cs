using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>Creates an <see cref="ApplicationDbContext"/> for <c>dotnet ef</c>.</summary>
/// <remarks>
/// Lets the tooling generate DDL without booting the application host. Set
/// <c>ConnectionStrings__TaskManagement</c> to point <c>database update</c> at a real database;
/// <c>migrations add</c> and <c>migrations script</c> never open a connection. There is no fallback
/// on purpose: a default would quietly run DDL against whatever database it named.
/// </remarks>
public sealed class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Binds ConnectionStrings__TaskManagement the same way the host does.
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

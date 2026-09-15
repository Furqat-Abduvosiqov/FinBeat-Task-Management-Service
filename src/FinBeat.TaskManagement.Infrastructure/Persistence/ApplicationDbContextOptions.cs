using System.Globalization;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>Builds and configures the pieces an <see cref="ApplicationDbContext"/> needs.</summary>
/// <remarks>
/// Runtime DI, the design-time factory (<see cref="DesignTimeApplicationDbContextFactory"/>) and the test suites
/// all come through here rather than building a <see cref="DbContextOptionsBuilder"/> by hand. A second,
/// hand-rolled options builder is exactly how a schema ends up migrated in PascalCase and queried in snake_case —
/// a mismatch that compiles cleanly and then surfaces as <c>relation "tasks" does not exist</c>, nowhere near
/// whatever built the divergent options.
/// </remarks>
public static class ApplicationDbContextOptions
{
    /// <summary>Builds the <see cref="NpgsqlDataSource"/> a context reads and writes through.</summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="loggerFactory">
    /// Logging for the data source, or <see langword="null"/> where none is wired up, such as at design time.
    /// </param>
    /// <returns>A data source with <see cref="TaskItemStatus"/> mapped to its native PostgreSQL enum.</returns>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is null, empty, or white space.</exception>
    /// <remarks>
    /// A data source loads PostgreSQL's type catalogue once, on its first physical connection, and caches it for
    /// its own lifetime. One that first connects before <c>CREATE TYPE task_item_status</c> has run never sees the
    /// type, and every insert through it fails from then on — not just the first time. So whatever applies the
    /// migration must not be the same long-lived data source that later reads and writes rows: migrate, dispose,
    /// then build the one the application keeps.
    /// </remarks>
    public static NpgsqlDataSource CreateDataSource(string connectionString, ILoggerFactory? loggerFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TaskItemStatus>(PostgresEnumMapping.TaskItemStatusTypeName);

        if (loggerFactory is not null)
        {
            dataSourceBuilder.UseLoggerFactory(loggerFactory);
        }

        return dataSourceBuilder.Build();
    }

    /// <summary>Configures a context against a <see cref="NpgsqlDataSource"/> the caller owns.</summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="dataSource">The data source, built by <see cref="CreateDataSource"/>.</param>
    public static void Configure(DbContextOptionsBuilder builder, NpgsqlDataSource dataSource)
    {
        builder.UseNpgsql(dataSource);
        ApplyNamingConvention(builder);
    }

    /// <summary>Configures a context from a connection string, building and owning the data source itself.</summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is null, empty, or white space.</exception>
    /// <remarks>
    /// Identical to <see cref="Configure"/> in every respect but one: the data source is created here rather than
    /// passed in, and nothing disposes it. For a <c>dotnet ef</c> invocation that is the whole process lifetime and
    /// costs nothing. A caller that goes on to read and write rows in the same process — the test fixture does —
    /// wants <see cref="Configure"/> with a data source it can dispose, or it inherits a type catalogue cached
    /// before the migration created the enum.
    /// </remarks>
    public static void ConfigureFromConnectionString(DbContextOptionsBuilder builder, string connectionString) =>
        Configure(builder, CreateDataSource(connectionString));

    // Every path calls this, so the model is identical no matter which one built the options. Calling it before or
    // after UseNpgsql makes no difference: each adds its own options extension, and neither reads the other's.
    private static void ApplyNamingConvention(DbContextOptionsBuilder builder) =>
        builder.UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
}

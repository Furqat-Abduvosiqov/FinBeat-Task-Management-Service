using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

/// <summary>
/// A throwaway PostgreSQL instance, migrated once and shared by every <c>RequiresDocker</c> test
/// through <see cref="PostgresCollection"/>.
/// </summary>
/// <remarks>
/// The three steps in <see cref="InitializeAsync"/> have to happen in exactly that order. An
/// <see cref="NpgsqlDataSource"/> loads PostgreSQL's type catalogue once, on its first physical
/// connection, and caches it for its whole lifetime. Built before the migration runs
/// <c>CREATE TYPE task_item_status</c>, it would never see that type, and every insert afterwards
/// would fail with an unmapped-type error pointing nowhere near the real cause.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private NpgsqlDataSource? _dataSource;

    /// <summary>The data source the tests use to run raw SQL assertions alongside EF Core.</summary>
    public NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    /// <summary>Starts the container, migrates it, and only then builds the pooled data source.</summary>
    public async Task InitializeAsync()
    {
        // 1. Start the container.
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        // 2. Migrate through a context built from a plain connection string - no pooled
        // NpgsqlDataSource yet - so CREATE TYPE task_item_status is the first thing to touch the
        // connection, before anything could have cached a type catalogue without it.
        var schemaOptions = new DbContextOptionsBuilder<ApplicationDbContext>();
        ApplicationDbContextOptions.ConfigureFromConnectionString(schemaOptions, connectionString);

        await using (var schemaContext = new ApplicationDbContext(schemaOptions.Options))
        {
            await schemaContext.Database.MigrateAsync();

            var pendingMigrations = await schemaContext.Database.GetPendingMigrationsAsync();
            pendingMigrations.ShouldBeEmpty();
        }

        // 3. Only now build the data source the tests actually use.
        _dataSource = ApplicationDbContextOptions.CreateDataSource(connectionString);
    }

    /// <summary>Builds a fresh <see cref="ApplicationDbContext"/> over the shared data source.</summary>
    /// <remarks>
    /// A new context - and, since the data source pools connections rather than reusing one, typically
    /// a fresh connection - per call. Nothing is served from an identity map left over from a previous
    /// read, which is exactly where a stale enum type cache would show up.
    /// </remarks>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        ApplicationDbContextOptions.Configure(options, DataSource);

        return new ApplicationDbContext(options.Options);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

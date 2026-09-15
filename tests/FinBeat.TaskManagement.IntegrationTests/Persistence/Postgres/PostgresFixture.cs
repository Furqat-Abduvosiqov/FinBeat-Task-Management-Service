using FinBeat.TaskManagement.Domain.Tasks;
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
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private NpgsqlDataSource? _dataSource;
    private string? _connectionString;

    /// <summary>The data source the tests use for raw SQL assertions alongside EF Core.</summary>
    public NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    /// <summary>Starts the container and applies the migrations.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _connectionString = _container.GetConnectionString();
        _dataSource = NpgsqlDataSource.Create(_connectionString);

        await using var schemaContext = CreateContext();
        await schemaContext.Database.MigrateAsync();

        var pendingMigrations = await schemaContext.Database.GetPendingMigrationsAsync();
        pendingMigrations.ShouldBeEmpty();
    }

    /// <summary>Builds a fresh <see cref="ApplicationDbContext"/> against the container.</summary>
    /// <remarks>
    /// A new context per call, so nothing is served from an identity map left over from an earlier
    /// read — which is what a round-trip assertion has to rule out in order to mean anything.
    /// </remarks>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseNpgsql(_connectionString ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet."));

        return new ApplicationDbContext(options.Options);
    }

    /// <summary>Adds the given tasks through a fresh context and saves them.</summary>
    /// <remarks>
    /// Fresh, and disposed before returning, rather than a context the caller keeps for the read that
    /// usually follows a seed: one context spanning both would serve that read out of its own identity
    /// map instead of out of PostgreSQL, which is exactly the failure a round-trip test exists to catch.
    /// </remarks>
    public async Task SeedAsync(params TaskItem[] tasks)
    {
        await using var context = CreateContext();
        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
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

using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

/// <summary>A throwaway PostgreSQL instance, migrated once and shared by every <c>RequiresDocker</c> test through <see cref="PostgresCollection"/>.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage(TestImages.PostgreSql)
        .Build();

    private NpgsqlDataSource? _dataSource;
    private string? _connectionString;

    /// <summary>The data source the tests use for raw SQL assertions alongside EF Core.</summary>
    public NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    /// <summary>The migrated container's connection string, for tests that build their own container from <c>AddInfrastructure</c>.</summary>
    public string ConnectionString =>
        _connectionString ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    /// <summary>Starts the container and applies the migrations.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _connectionString = _container.GetConnectionString();
        _dataSource = NpgsqlDataSource.Create(_connectionString);

        await using var schemaContext = CreateContext();
        await schemaContext.Database.MigrateAsync();
    }

    /// <summary>Builds a fresh <see cref="ApplicationDbContext"/> against the container.</summary>
    /// <remarks>A new context per call, so nothing is served from a stale identity map — what a round-trip assertion has to rule out.</remarks>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseNpgsql(_connectionString ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet."));

        return new ApplicationDbContext(options.Options);
    }

    /// <summary>Adds the given tasks through a fresh context and saves them.</summary>
    /// <remarks>Disposed before returning: a context spanning seed and read would serve the read from its own identity map instead of PostgreSQL.</remarks>
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

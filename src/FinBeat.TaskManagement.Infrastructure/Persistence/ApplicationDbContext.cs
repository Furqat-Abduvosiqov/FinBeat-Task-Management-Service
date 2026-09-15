using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The EF Core <see cref="DbContext"/> this application persists through.</summary>
/// <param name="options">The context options.</param>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    /// <summary>The table EF records applied migrations in.</summary>
    /// <remarks>Named explicitly because EF names the default explicitly too, at a configuration source the naming convention cannot override - so the table would stay <c>__EFMigrationsHistory</c> while its own columns became <c>migration_id</c> and <c>product_version</c>.</remarks>
    public const string MigrationsHistoryTableName = "__ef_migrations_history";

    /// <summary>The tasks. This property name is what names the table <c>tasks</c>.</summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <inheritdoc />
    /// <remarks>Applied here so no caller that builds options can leave it out - the host, the
    /// design-time factory, the migrator and the test fixture all build their own options.</remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.UseNpgsql(npgsql =>
        {
            npgsql.MigrationsHistoryTable(MigrationsHistoryTableName);

            // A restarted or failed-over server terminates its backends. Without a retrying strategy the
            // next use of a pooled connection becomes a 500 the caller can do nothing about.
            npgsql.EnableRetryOnFailure();
        });

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // MassTransit's transactional outbox. These tables have to live in this context, because that
        // is what lets a publish and the state change that caused it share one transaction.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}

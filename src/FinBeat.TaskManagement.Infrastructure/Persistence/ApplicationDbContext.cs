using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The EF Core <see cref="DbContext"/> this application persists through.</summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">The context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>The tasks. This property name is what names the table <c>tasks</c>.</summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <inheritdoc />
    /// <remarks>Applied here so no caller that builds options can leave it out.</remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.UseSnakeCaseNamingConvention();

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // MassTransit's transactional outbox. These tables have to live in this context, because that
        // is what lets a publish and the state change that caused it share one transaction.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}

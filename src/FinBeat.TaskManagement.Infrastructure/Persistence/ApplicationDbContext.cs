using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FinBeat.TaskManagement.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The EF Core <see cref="DbContext"/> this application persists through.</summary>
/// <remarks>
/// <para>
/// No <c>SaveChangesAsync</c> override appears here. <see cref="DbContext"/> already declares
/// <c>Task&lt;int&gt; SaveChangesAsync(CancellationToken)</c>, which satisfies
/// <see cref="IApplicationDbContext"/> implicitly — there is nothing to add yet.
/// </para>
/// <para>
/// This is also where a future domain-event dispatch belongs, and it will have to read the change
/// tracker <em>before</em> calling into the base save, not after: EF detaches a deleted entity once
/// the save completes, and a detached entity's events are unreachable by the time anything could
/// read them back.
/// </para>
/// </remarks>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Creates the context with the options <see cref="ApplicationDbContextOptions"/> built.</summary>
    /// <param name="options">The context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>The tasks.</summary>
    /// <remarks>The property name is load-bearing: with no explicit <c>ToTable</c> call, it is what names the table <c>tasks</c>.</remarks>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // The EF-model half of the enum mapping: this annotation is what puts CREATE TYPE
        // task_item_status into the migration. The ADO.NET half lives in
        // ApplicationDbContextOptions.CreateDataSource, and the two are tied together by sharing
        // PostgresEnumMapping's single type name.
        modelBuilder.HasPostgresEnum<TaskItemStatus>(name: PostgresEnumMapping.TaskItemStatusTypeName);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Registering these here is what makes EF treat TaskItemId, TaskTitle and TaskDescription as
        // scalars at all. Without it, the relationship convention takes a reference type it does not
        // recognise for a navigation property, and model building fails with something like "the
        // entity type 'TaskTitle' requires a primary key" instead of a converter-related message.
        configurationBuilder.Properties<TaskItemId>().HaveConversion<TaskItemIdConverter>();

        configurationBuilder.Properties<TaskTitle>()
            .HaveConversion<TaskTitleConverter>()
            .HaveMaxLength(TaskTitle.MaxLength);

        configurationBuilder.Properties<TaskDescription>()
            .HaveConversion<TaskDescriptionConverter>()
            .HaveMaxLength(TaskDescription.MaxLength);

        base.ConfigureConventions(configurationBuilder);
    }
}

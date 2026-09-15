using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBeat.TaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="TaskItem"/> to the <c>tasks</c> table.</summary>
/// <remarks>Internal: <c>ApplyConfigurationsFromAssembly</c> discovers it by scanning the assembly, not by public API surface.</remarks>
internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Mandatory, not tidiness. DomainEvents is discovered as a property, has no type mapping, and
        // cannot be a navigation either because IDomainEvent is an interface. Model building fails
        // outright without this line.
        builder.Ignore(task => task.DomainEvents);

        builder.HasKey(task => task.Id);

        // Ids come from TaskItemId.New(), never from the database.
        builder.Property(task => task.Id).ValueGeneratedNever();

        builder.Property(task => task.Title).IsRequired();

        // NOT NULL, holding '' for TaskDescription.None. See TaskDescriptionConverter for why.
        builder.Property(task => task.Description).IsRequired();

        // No HasConversion, deliberately: the native task_item_status type is registered on the model
        // in OnModelCreating, and converting to int here would quietly replace it with an integer
        // column while still creating the now-unused type.
        builder.Property(task => task.Status).IsRequired();

        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.UpdatedAt).IsRequired();

        // One index: "the newest tasks in a given status". Its leftmost column also serves a plain
        // status filter, which is why there is no separate index on status alone.
        builder.HasIndex(task => new { task.Status, task.CreatedAt });
    }
}

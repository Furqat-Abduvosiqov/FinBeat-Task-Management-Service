using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBeat.TaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="TaskItem"/> to the <c>tasks</c> table.</summary>
internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Model building fails without this: IDomainEvent is an interface, so EF can map DomainEvents
        // neither as a scalar nor as a navigation.
        builder.Ignore(task => task.DomainEvents);

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasConversion(id => id.Value, value => new TaskItemId(value))
            .ValueGeneratedNever();

        builder.Property(task => task.Title)
            .HasConversion(title => title.Value, value => TaskTitle.Create(value))
            .HasMaxLength(TaskTitle.MaxLength)
            .IsRequired();

        // TaskDescription.None is stored as an empty string. EF never passes null through a converter,
        // so a nullable column would materialise Description as null.
        builder.Property(task => task.Description)
            .HasConversion(description => description.Value, value => TaskDescription.Create(value))
            .HasMaxLength(TaskDescription.MaxLength)
            .IsRequired();

        builder.Property(task => task.Status).IsRequired();
        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.UpdatedAt).IsRequired();

        var declaredStatuses = string.Join(", ", Enum.GetValues<TaskItemStatus>().Select(status => (int)status));

        builder.ToTable(table =>
            table.HasCheckConstraint("ck_tasks_status", $"status IN ({declaredStatuses})"));

        builder.HasIndex(task => new { task.Status, task.CreatedAt });
    }
}

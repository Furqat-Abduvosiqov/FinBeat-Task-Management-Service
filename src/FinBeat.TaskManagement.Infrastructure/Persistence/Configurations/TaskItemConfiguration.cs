using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBeat.TaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="TaskItem"/> to the <c>tasks</c> table.</summary>
/// <remarks>
/// <para>
/// Internal: <c>ApplyConfigurationsFromAssembly</c> discovers it by scanning the assembly, not by
/// public API surface.
/// </para>
/// <para>
/// Every mapping decision for this aggregate is in this one file. The value-object conversions could
/// instead be registered per type in <c>ConfigureConventions</c>, which would tie each column width to
/// its domain constant once for all future entities — but that needs a named <c>ValueConverter</c>
/// class per value object, because the conventions builder has no lambda overload. With one entity,
/// reading the whole mapping in one place wins. Worth naming the trade: forgetting a conversion on a
/// future entity fails loudly at model build, but forgetting <c>HasMaxLength</c> silently yields
/// <c>text</c>.
/// </para>
/// </remarks>
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

        // Read back through the constructor rather than TaskItemId.From: a converter is a mapping, not
        // a factory. From throws on an empty guid, which would surface a bad row as an exception from
        // inside the materialiser with no row to point at — and it is not even a real guard, since an
        // outer join projects a missing key as default without consulting the converter at all. From
        // stays the guard at the edge that parses a guid. Ids come from TaskItemId.New(), never from
        // the database, hence ValueGeneratedNever.
        builder.Property(task => task.Id)
            .HasConversion(id => id.Value, value => new TaskItemId(value))
            .ValueGeneratedNever();

        // Create on the way back because the constructor is private. It trims a value that was stored
        // already trimmed, so it is idempotent and the round trip is lossless.
        builder.Property(task => task.Title)
            .HasConversion(title => title.Value, value => TaskTitle.Create(value))
            .HasMaxLength(TaskTitle.MaxLength)
            .IsRequired();

        // NOT NULL, storing '' for TaskDescription.None. EF never passes null through a value
        // converter, so a nullable column holding NULL would materialise Description = null and break
        // the "absence is None, not null" contract in the one place the aggregate cannot defend itself.
        // Create returns the None singleton for an empty string, so the round trip is
        // reference-identical rather than merely equal.
        builder.Property(task => task.Description)
            .HasConversion(description => description.Value, value => TaskDescription.Create(value))
            .HasMaxLength(TaskDescription.MaxLength)
            .IsRequired();

        // No HasConversion: an int-backed enum already maps to integer by default, so spelling it out
        // adds a line that changes nothing in the model. TaskItemStatus is explicitly numbered New = 1
        // through Archived = 4, which is what makes those numbers a persistence contract rather than an
        // accident of declaration order — reordering the members cannot silently repoint existing rows.
        // The column type is pinned by a model test rather than by a no-op call here.
        builder.Property(task => task.Status).IsRequired();

        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.UpdatedAt).IsRequired();

        // An int column would otherwise accept any int, including the 0 the enum deliberately has no
        // member for. Generated from the enum rather than hand-listed, so adding a status moves the
        // model — which the migration-drift test then insists on a migration for.
        var statuses = string.Join(", ", Enum.GetValues<TaskItemStatus>().Select(status => (int)status));

        builder.ToTable(table =>
            table.HasCheckConstraint("ck_tasks_status", $"status IN ({statuses})"));

        // One index: "the newest tasks in a given status". Its leftmost column also serves a plain
        // status filter, which is why there is no separate index on status alone.
        builder.HasIndex(task => new { task.Status, task.CreatedAt });
    }
}

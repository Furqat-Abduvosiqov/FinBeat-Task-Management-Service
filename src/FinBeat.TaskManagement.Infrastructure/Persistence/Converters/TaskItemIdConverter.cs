using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinBeat.TaskManagement.Infrastructure.Persistence.Converters;

/// <summary>Converts <see cref="TaskItemId"/> to and from the <see cref="Guid"/> column that stores it.</summary>
/// <remarks>
/// <para>
/// The read direction calls the <see cref="TaskItemId(Guid)"/> constructor directly, not
/// <see cref="TaskItemId.From(Guid)"/>. A converter is a mapping, not a factory: if the constructor
/// were replaced with <c>From</c> and a row somehow held an empty guid, the resulting
/// <see cref="ArgumentException"/> would surface from inside the materialiser with no row to point
/// at — a worse failure than the one it was meant to catch.
/// </para>
/// <para>
/// It also would not even be a real guard. An outer-join projection that finds no matching row
/// yields <c>default(TaskItemId)</c> for the id column directly, without ever calling this converter.
/// <c>From</c> remains the guard that belongs at the edge that parses a <see cref="Guid"/> off the
/// wire, not here.
/// </para>
/// </remarks>
public sealed class TaskItemIdConverter : ValueConverter<TaskItemId, Guid>
{
    /// <summary>Creates the converter.</summary>
    public TaskItemIdConverter()
        : base(id => id.Value, value => new TaskItemId(value))
    {
    }
}

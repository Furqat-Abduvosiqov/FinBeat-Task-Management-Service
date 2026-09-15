namespace FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

/// <summary>Identifies a task.</summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>Assigned here, not by the database, so a task has an identity before it is saved.</remarks>
public readonly record struct TaskItemId(Guid Value)
{
    /// <summary>Creates a new unique id.</summary>
    public static TaskItemId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

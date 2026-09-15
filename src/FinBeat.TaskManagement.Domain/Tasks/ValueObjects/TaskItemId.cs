namespace FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

/// <summary>Identifies a task.</summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>
/// Ids are assigned here, not by the database, so a task has an identity before it is saved.
/// Aggregates compare by id, and a database-assigned id would leave every unsaved task holding
/// <see cref="Guid.Empty"/> — two new tasks would compare equal and a set would keep only one.
/// </remarks>
public readonly record struct TaskItemId(Guid Value)
{
    /// <summary>Creates a new unique id.</summary>
    public static TaskItemId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

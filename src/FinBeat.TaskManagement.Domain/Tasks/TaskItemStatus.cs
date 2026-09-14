namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// The lifecycle state of a <c>TaskItem</c>.
/// </summary>
/// <remarks>
/// The technical specification names three statuses: new, in progress, and completed (<c>новая</c>,
/// <c>в работе</c>, <c>выполненная</c>). <see cref="Archived"/> is a deliberate fourth: this project
/// uses hard delete rather than soft delete, and <see cref="Archived"/> is the retention mechanism
/// that replaces it — a user who wants to keep a task's history archives it instead of deleting it,
/// and only an explicit deletion ever removes the row.
/// </remarks>
public enum TaskItemStatus
{
    /// <summary>The task has been created and no work has started on it yet.</summary>
    New = 1,

    /// <summary>Work on the task is underway.</summary>
    InProgress = 2,

    /// <summary>The task's work is finished.</summary>
    Completed = 3,

    /// <summary>
    /// The task is retained for its history rather than deleted. See the remarks on
    /// <see cref="TaskItemStatus"/> for why this status exists.
    /// </summary>
    Archived = 4,
}

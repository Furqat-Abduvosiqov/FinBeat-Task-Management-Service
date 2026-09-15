namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>Which status changes a task is allowed to make.</summary>
/// <remarks>
/// Separate from the aggregate so the whole matrix fits one parameterized test instead of being
/// spread across several methods. Restoring an archived task lands in <see cref="TaskItemStatus.New"/>
/// rather than whatever it was before: recovering that would need a field the specification never
/// asks for, and the status-changed event already records it.
/// </remarks>
public static class TaskItemStatusRules
{
    /// <summary>Whether a task may move from one status to another.</summary>
    /// <returns>
    /// False for anything not listed below, including a move to the same status — the aggregate
    /// treats that as a no-op before it ever asks.
    /// </returns>
    public static bool CanTransition(TaskItemStatus from, TaskItemStatus to) => (from, to) switch
    {
        (TaskItemStatus.New, TaskItemStatus.InProgress or TaskItemStatus.Completed or TaskItemStatus.Archived) => true,
        (TaskItemStatus.InProgress, TaskItemStatus.New or TaskItemStatus.Completed or TaskItemStatus.Archived) => true,
        (TaskItemStatus.Completed, TaskItemStatus.InProgress or TaskItemStatus.Archived) => true,
        (TaskItemStatus.Archived, TaskItemStatus.New) => true,
        _ => false,
    };
}

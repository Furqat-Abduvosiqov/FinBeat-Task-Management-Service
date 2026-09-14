namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// The allowed transitions between <see cref="TaskItemStatus"/> values.
/// </summary>
/// <remarks>
/// <para>
/// This lives in its own static class, separate from <c>TaskItem</c>, so the aggregate stays
/// readable and the whole transition matrix can be covered by a single parameterized test instead
/// of being scattered across several of the aggregate's methods.
/// </para>
/// <para>
/// <see cref="CanTransition"/> deliberately returns <see langword="false"/> for a transition to the
/// same status. That is not an oversight: same-status changes are handled by the aggregate itself
/// as a no-op, before these rules are ever consulted, so the table below only needs to describe
/// genuine moves between different states.
/// </para>
/// <para>
/// Restoring an archived task lands back in <see cref="TaskItemStatus.New"/> rather than the status
/// it held before being archived. Preserving that status would need a separate
/// <c>StatusBeforeArchive</c> field the specification never asks for, and the information is not
/// actually lost: <c>TaskItemStatusChangedDomainEvent</c> carries <c>PreviousStatus</c>, so the
/// event log already records that a task was, say, <see cref="TaskItemStatus.Completed"/> at the
/// moment it was archived.
/// </para>
/// </remarks>
public static class TaskItemStatusRules
{
    /// <summary>Determines whether a task may move from <paramref name="from"/> to <paramref name="to"/>.</summary>
    /// <param name="from">The task's current status.</param>
    /// <param name="to">The status the task would move to.</param>
    /// <returns>
    /// <see langword="true"/> when the transition is allowed; <see langword="false"/> for every
    /// transition not explicitly listed below, including a transition to the same status.
    /// </returns>
    public static bool CanTransition(TaskItemStatus from, TaskItemStatus to) => (from, to) switch
    {
        (TaskItemStatus.New, TaskItemStatus.InProgress) => true,
        (TaskItemStatus.New, TaskItemStatus.Completed) => true,
        (TaskItemStatus.New, TaskItemStatus.Archived) => true,

        (TaskItemStatus.InProgress, TaskItemStatus.Completed) => true,
        (TaskItemStatus.InProgress, TaskItemStatus.New) => true,
        (TaskItemStatus.InProgress, TaskItemStatus.Archived) => true,

        (TaskItemStatus.Completed, TaskItemStatus.InProgress) => true,
        (TaskItemStatus.Completed, TaskItemStatus.Archived) => true,

        (TaskItemStatus.Archived, TaskItemStatus.New) => true,

        _ => false,
    };
}

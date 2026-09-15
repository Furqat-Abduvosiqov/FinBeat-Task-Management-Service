using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>A status change the rules do not allow.</summary>
/// <param name="from">The status the task was in.</param>
/// <param name="to">The status it tried to move to.</param>
public sealed class InvalidTaskStatusTransitionException(TaskItemStatus from, TaskItemStatus to)
    : DomainException($"Cannot transition a task from '{from}' to '{to}'.")
{
    /// <summary>The status the task was in.</summary>
    public TaskItemStatus From { get; } = from;

    /// <summary>The status it tried to move to.</summary>
    public TaskItemStatus To { get; } = to;

    /// <inheritdoc />
    public override string ErrorCode => "task.status.invalid-transition";
}

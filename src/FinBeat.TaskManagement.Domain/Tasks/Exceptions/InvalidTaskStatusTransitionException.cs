using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>Raised when a status change is not allowed by <see cref="TaskItemStatusRules"/>.</summary>
public sealed class InvalidTaskStatusTransitionException : DomainException
{
    /// <summary>Initializes a new instance for a rejected transition between two statuses.</summary>
    /// <param name="from">The status the task was in when the transition was attempted.</param>
    /// <param name="to">The status the transition attempted to move the task to.</param>
    public InvalidTaskStatusTransitionException(TaskItemStatus from, TaskItemStatus to)
        : base($"Cannot transition a task from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }

    /// <summary>The status the task was in when the transition was attempted.</summary>
    public TaskItemStatus From { get; }

    /// <summary>The status the transition attempted to move the task to.</summary>
    public TaskItemStatus To { get; }

    /// <inheritdoc />
    public override string ErrorCode => "task.status.invalid-transition";
}

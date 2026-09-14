using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>A status change the rules do not allow.</summary>
public sealed class InvalidTaskStatusTransitionException : DomainException
{
    /// <summary>Creates the exception for a rejected move between two statuses.</summary>
    public InvalidTaskStatusTransitionException(TaskItemStatus from, TaskItemStatus to)
        : base($"Cannot transition a task from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }

    /// <summary>The status the task was in.</summary>
    public TaskItemStatus From { get; }

    /// <summary>The status it tried to move to.</summary>
    public TaskItemStatus To { get; }

    /// <inheritdoc />
    public override string ErrorCode => "task.status.invalid-transition";
}

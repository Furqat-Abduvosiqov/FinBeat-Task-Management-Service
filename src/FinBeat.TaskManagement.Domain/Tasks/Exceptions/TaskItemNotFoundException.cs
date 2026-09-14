using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>Raised when a task lookup by id finds no matching task.</summary>
public sealed class TaskItemNotFoundException : DomainException
{
    /// <summary>Initializes a new instance for a task id that could not be found.</summary>
    /// <param name="taskId">The id that was looked up.</param>
    public TaskItemNotFoundException(TaskItemId taskId)
        : base($"No task was found with id '{taskId}'.")
    {
        TaskId = taskId;
    }

    /// <summary>The id that was looked up.</summary>
    public TaskItemId TaskId { get; }

    /// <inheritdoc />
    public override string ErrorCode => "task.not-found";
}

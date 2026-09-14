using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>No task exists with the given id.</summary>
public sealed class TaskItemNotFoundException : DomainException
{
    /// <summary>Creates the exception for an id that was not found.</summary>
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

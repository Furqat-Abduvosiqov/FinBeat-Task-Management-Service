using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Application.Tasks;

/// <summary>The failures a task use case can report.</summary>
public static class TaskErrors
{
    /// <summary>No task exists with the given id.</summary>
    /// <param name="taskId">The id that was looked up.</param>
    public static Error NotFound(Guid taskId) =>
        new("task.not-found", $"No task was found with id '{taskId}'.", ErrorType.NotFound);

    /// <summary>Translates a domain exception, keeping the error code the domain assigned.</summary>
    /// <param name="exception">The exception a domain call raised.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    public static Error FromDomain(DomainException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new Error(exception.ErrorCode, exception.Message, Classify(exception));
    }

    private static ErrorType Classify(DomainException exception) => exception switch
    {
        TaskItemNotFoundException => ErrorType.NotFound,
        InvalidTaskStatusTransitionException => ErrorType.Conflict,
        _ => ErrorType.Validation,
    };
}

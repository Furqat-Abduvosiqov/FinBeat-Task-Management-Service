using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Application.Tasks;

/// <summary>The failures a task use case can report.</summary>
public static class TaskErrors
{
    /// <summary>No task exists with the given id.</summary>
    /// <param name="taskId">The id that was looked up.</param>
    public static Error NotFound(Guid taskId) =>
        new("task.not-found", $"No task was found with id '{taskId}'.", ErrorType.NotFound);

    /// <summary>The page asked for is outside what a caller may request.</summary>
    /// <param name="maxPageSize">The largest page size on offer.</param>
    public static Error InvalidPaging(int maxPageSize) =>
        new(
            "task.paging.invalid",
            $"Page must be 1 or greater and page size between 1 and {maxPageSize}.",
            ErrorType.Validation);

    /// <summary>Someone else changed the task between this request reading it and saving.</summary>
    /// <param name="taskId">The task that moved underneath the request.</param>
    public static Error ConcurrentlyModified(Guid taskId) =>
        new(
            "task.concurrent-modification",
            $"Task '{taskId}' was changed by someone else while this request was in flight.",
            ErrorType.Conflict);

    /// <summary>The status supplied is not one the domain declares.</summary>
    /// <param name="status">The value that was supplied.</param>
    /// <remarks>A value outside the enum would otherwise reach the transition rules, be rejected as a move nobody can make, and come back as a conflict rather than the bad request it is.</remarks>
    public static Error UnknownStatus(TaskItemStatus status) =>
        new("task.status.unknown", $"'{(int)status}' is not a known task status.", ErrorType.Validation);

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
        InvalidTaskStatusTransitionException => ErrorType.Conflict,
        _ => ErrorType.Validation,
    };
}

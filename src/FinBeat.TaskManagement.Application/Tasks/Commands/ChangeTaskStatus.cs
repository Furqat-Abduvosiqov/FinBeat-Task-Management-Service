using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;

namespace FinBeat.TaskManagement.Application.Tasks.Commands;

/// <summary>Asks to move a task to another status.</summary>
/// <param name="TaskId">The task to move.</param>
/// <param name="Status">The status to move it to.</param>
public sealed record ChangeTaskStatusCommand(Guid TaskId, TaskItemStatus Status);

/// <summary>Moves a task through its lifecycle, archiving and restoring included.</summary>
/// <param name="context">The database the task is read from and written to.</param>
/// <param name="publisher">Where the resulting integration event goes.</param>
/// <param name="clock">The source of the modification timestamp.</param>
public sealed class ChangeTaskStatusHandler(
    IApplicationDbContext context,
    IIntegrationEventPublisher publisher,
    TimeProvider clock)
{
    /// <summary>Applies the status change.</summary>
    /// <param name="command">The task, and the status to move it to.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The updated task, or an error if it does not exist or the move is not allowed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is null.</exception>
    public async Task<Result<TaskResponse>> HandleAsync(
        ChangeTaskStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.IsDefined(command.Status))
        {
            return Result.Failure<TaskResponse>(TaskErrors.UnknownStatus(command.Status));
        }

        var task = await context.Tasks.FindByIdAsync(command.TaskId, cancellationToken);

        if (task is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound(command.TaskId));
        }

        try
        {
            task.ChangeStatus(command.Status, clock);
        }
        catch (DomainException exception)
        {
            return Result.Failure<TaskResponse>(TaskErrors.FromDomain(exception));
        }

        await publisher.PublishRaisedEventsAsync(task, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(TaskResponse.From(task));
    }
}

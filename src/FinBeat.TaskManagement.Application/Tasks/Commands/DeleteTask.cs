using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks.Commands;

/// <summary>Asks for a task to be deleted.</summary>
/// <param name="TaskId">The task to delete.</param>
/// <remarks>The row goes for good. Move a task to <see cref="TaskItemStatus.Archived"/> to keep it instead.</remarks>
public sealed record DeleteTaskCommand(Guid TaskId);

/// <summary>Deletes a task and announces it.</summary>
/// <param name="context">The database the task is removed from.</param>
/// <param name="publisher">Where the resulting integration event goes.</param>
/// <param name="clock">The source of the deletion timestamp.</param>
public sealed class DeleteTaskHandler(
    IApplicationDbContext context,
    IIntegrationEventPublisher publisher,
    TimeProvider clock)
{
    /// <summary>Deletes the task.</summary>
    /// <param name="command">The task to delete.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Success, or a not-found error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is null.</exception>
    public async Task<Result> HandleAsync(
        DeleteTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await context.Tasks.FindByIdAsync(command.TaskId, cancellationToken);

        if (task is null)
        {
            return Result.Failure(TaskErrors.NotFound(command.TaskId));
        }

        task.Delete(clock);
        context.Tasks.Remove(task);

        await publisher.PublishRaisedEventsAsync(task, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Nobody else could have changed it into existence, so it was deleted under us.
            return Result.Failure(TaskErrors.NotFound(command.TaskId));
        }

        return Result.Success();
    }
}

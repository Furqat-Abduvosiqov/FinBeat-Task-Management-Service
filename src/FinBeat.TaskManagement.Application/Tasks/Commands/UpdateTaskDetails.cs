using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks.Commands;

/// <summary>Asks to change a task's title and description.</summary>
/// <param name="TaskId">The task to change.</param>
/// <param name="Title">The new title. Required.</param>
/// <param name="Description">The new description. Optional: null or whitespace means none.</param>
public sealed record UpdateTaskDetailsCommand(Guid TaskId, string? Title, string? Description);

/// <summary>Changes a task's title and description.</summary>
/// <param name="context">The database the task is read from and written to.</param>
/// <param name="publisher">Where the resulting integration event goes.</param>
/// <param name="clock">The source of the modification timestamp.</param>
public sealed class UpdateTaskDetailsHandler(
    IApplicationDbContext context,
    IIntegrationEventPublisher publisher,
    TimeProvider clock)
{
    /// <summary>Applies the change.</summary>
    /// <param name="command">The task, and the values to give it.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The updated task, or an error if it does not exist or the values were rejected.</returns>
    public async Task<Result<TaskResponse>> HandleAsync(
        UpdateTaskDetailsCommand command,
        CancellationToken cancellationToken = default)
    {
        var task = await context.Tasks.FindByIdAsync(command.TaskId, cancellationToken);

        if (task is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound(command.TaskId));
        }

        try
        {
            task.UpdateDetails(
                TaskTitle.Create(command.Title),
                TaskDescription.Create(command.Description),
                clock);
        }
        catch (DomainException exception)
        {
            return Result.Failure<TaskResponse>(TaskErrors.FromDomain(exception));
        }

        await publisher.PublishRaisedEventsAsync(task, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<TaskResponse>(TaskErrors.ConcurrentlyModified(command.TaskId));
        }

        return Result.Success(TaskResponse.From(task));
    }
}

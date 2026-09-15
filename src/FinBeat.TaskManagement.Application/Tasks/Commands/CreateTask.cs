using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks.Commands;

/// <summary>Asks for a new task.</summary>
/// <param name="Title">The title. Required, and trimmed to at most <see cref="TaskTitle.MaxLength"/> characters.</param>
/// <param name="Description">The description. Optional: null or whitespace means none.</param>
public sealed record CreateTaskCommand(string? Title, string? Description);

/// <summary>Creates a task and announces it.</summary>
/// <param name="context">The database the task is written to.</param>
/// <param name="publisher">Where the resulting integration event goes.</param>
/// <param name="clock">The source of the creation timestamp.</param>
public sealed class CreateTaskHandler(
    IApplicationDbContext context,
    IIntegrationEventPublisher publisher,
    TimeProvider clock)
{
    /// <summary>Creates the task.</summary>
    /// <param name="command">What to create it with.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The task that was created, or a validation error saying why it was rejected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is null.</exception>
    public async Task<Result<TaskResponse>> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        TaskItem task;

        try
        {
            task = TaskItem.Create(
                TaskTitle.Create(command.Title),
                TaskDescription.Create(command.Description),
                clock);
        }
        catch (DomainException exception)
        {
            return Result.Failure<TaskResponse>(TaskErrors.FromDomain(exception));
        }

        context.Tasks.Add(task);

        await publisher.PublishRaisedEventsAsync(task, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The retrying execution strategy re-sends this INSERT when the first attempt's commit reached
            // the server but its acknowledgement did not come back - the restart case EnableRetryOnFailure
            // exists for. The id is assigned in the domain, not by the database, so the replay carries the
            // same primary key and Postgres rejects it as a unique violation, which is not transient. If a
            // row now exists under this id, the first attempt committed and nobody else could have produced
            // that id, so the create is done. AsNoTracking because the failed save left this task tracked as
            // Added under the same key and a tracking query would resolve to that instance. The SQLSTATE
            // itself is out of reach here - Application references EF Core and nothing else - so the probe
            // stands in for 23505, and anything the probe does not explain is rethrown unchanged.
            var committed = await context.Tasks
                .AsNoTracking()
                .FindByIdAsync(task.Id.Value, cancellationToken);

            if (committed is null)
            {
                throw;
            }

            return Result.Success(TaskResponse.From(task));
        }

        return Result.Success(TaskResponse.From(task));
    }
}

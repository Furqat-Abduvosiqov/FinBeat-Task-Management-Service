using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks.Queries;

/// <summary>Asks for one task.</summary>
/// <param name="TaskId">The task to read.</param>
public sealed record GetTaskByIdQuery(Guid TaskId);

/// <summary>Reads one task.</summary>
/// <param name="context">The database the task is read from.</param>
public sealed class GetTaskByIdHandler(IApplicationDbContext context)
{
    /// <summary>Reads the task.</summary>
    /// <param name="query">The task to read.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The task, or a not-found error.</returns>
    public async Task<Result<TaskResponse>> HandleAsync(
        GetTaskByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var task = await context.Tasks.AsNoTracking().FindByIdAsync(query.TaskId, cancellationToken);

        return task is null
            ? Result.Failure<TaskResponse>(TaskErrors.NotFound(query.TaskId))
            : Result.Success(TaskResponse.From(task));
    }
}

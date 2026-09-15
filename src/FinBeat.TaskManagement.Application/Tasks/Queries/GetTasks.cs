using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks.Queries;

/// <summary>Asks for the tasks, oldest first.</summary>
/// <param name="Status">Return only tasks in this status, or null for all of them.</param>
public sealed record GetTasksQuery(TaskItemStatus? Status = null);

/// <summary>Reads the tasks.</summary>
/// <param name="context">The database the tasks are read from.</param>
public sealed class GetTasksHandler(IApplicationDbContext context)
{
    /// <summary>Reads the tasks.</summary>
    /// <param name="query">The status to filter by, if any.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching tasks, oldest first.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is null.</exception>
    public async Task<Result<IReadOnlyList<TaskResponse>>> HandleAsync(
        GetTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tasks = context.Tasks.AsNoTracking();

        // Filter then order: ix_tasks_status_created_at serves exactly this shape with no sort step.
        if (query.Status is { } status)
        {
            tasks = tasks.Where(task => task.Status == status);
        }

        var matches = await tasks.OrderBy(task => task.CreatedAt).ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TaskResponse>>(matches.ConvertAll(TaskResponse.From));
    }
}

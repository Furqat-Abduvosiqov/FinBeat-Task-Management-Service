using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks.Queries;

/// <summary>Asks for a page of tasks, oldest first.</summary>
/// <param name="Status">Return only tasks in this status, or null for all of them.</param>
/// <param name="PageNumber">Which page to return, counting from one.</param>
/// <param name="PageSize">How many tasks to return, at most <see cref="GetTasksHandler.MaxPageSize"/>.</param>
public sealed record GetTasksQuery(
    TaskItemStatus? Status = null,
    int PageNumber = 1,
    int PageSize = GetTasksHandler.DefaultPageSize);

/// <summary>Reads a page of tasks.</summary>
/// <param name="context">The database the tasks are read from.</param>
public sealed class GetTasksHandler(IApplicationDbContext context)
{
    /// <summary>The page size used when a caller does not ask for one.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The largest page a caller may ask for.</summary>
    /// <remarks>A ceiling rather than a clamp: silently returning fewer rows than asked for is harder to notice than being told no.</remarks>
    public const int MaxPageSize = 100;

    /// <summary>Reads the page.</summary>
    /// <param name="query">The status to filter by, and which page to return.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching page, oldest first, or a validation error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is null.</exception>
    public async Task<Result<Page<TaskResponse>>> HandleAsync(
        GetTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Status is { } requested && !Enum.IsDefined(requested))
        {
            return Result.Failure<Page<TaskResponse>>(TaskErrors.UnknownStatus(requested));
        }

        if (query.PageNumber < 1 || query.PageSize < 1 || query.PageSize > MaxPageSize)
        {
            return Result.Failure<Page<TaskResponse>>(TaskErrors.InvalidPaging(MaxPageSize));
        }

        var tasks = context.Tasks.AsNoTracking();

        // Filter then order: ix_tasks_status_created_at serves exactly this shape with no sort step.
        if (query.Status is { } status)
        {
            tasks = tasks.Where(task => task.Status == status);
        }

        var totalItems = await tasks.LongCountAsync(cancellationToken);

        var matches = await tasks
            // Id breaks ties, since CreatedAt is not unique and two pages could otherwise overlap.
            // Defensive: PostgreSQL happens to be stable here, so no test can fail on it.
            .OrderBy(task => task.CreatedAt)
            .ThenBy(task => task.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new Page<TaskResponse>(
            matches.ConvertAll(TaskResponse.From),
            query.PageNumber,
            query.PageSize,
            totalItems));
    }
}

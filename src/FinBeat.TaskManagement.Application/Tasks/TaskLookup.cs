using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks;

internal static class TaskLookup
{
    // The TaskItemId constructor rather than From: From rejects an empty guid by throwing, and a
    // caller who sends one has earned the same "not found" as any other id with no row behind it.
    internal static Task<TaskItem?> FindByIdAsync(
        this IQueryable<TaskItem> tasks,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var id = new TaskItemId(taskId);

        return tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }
}

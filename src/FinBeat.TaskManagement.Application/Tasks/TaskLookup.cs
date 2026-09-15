using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Tasks;

internal static class TaskLookup
{
    // No validation: Guid.Empty simply matches no row, and not-found is the honest answer.
    internal static Task<TaskItem?> FindByIdAsync(
        this IQueryable<TaskItem> tasks,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var id = new TaskItemId(taskId);

        return tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }
}

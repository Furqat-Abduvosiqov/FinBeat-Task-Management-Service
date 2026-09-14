namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>A collection of tasks.</summary>
/// <remarks>
/// No <c>Update</c> — EF tracks changes made through the aggregate's own methods. No
/// <c>SaveChangesAsync</c> either: committing is a unit of work, which belongs to Application. A
/// repository is a collection of aggregates; you can describe it without saying "database".
/// </remarks>
public interface ITaskItemRepository
{
    /// <summary>Finds a task, or null if there is no such task.</summary>
    Task<TaskItem?> GetByIdAsync(TaskItemId id, CancellationToken cancellationToken = default);

    /// <summary>Lists one page of tasks, optionally filtered by status.</summary>
    Task<IReadOnlyList<TaskItem>> ListAsync(TaskItemStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Adds a task, to be inserted on the next commit.</summary>
    void Add(TaskItem taskItem);

    /// <summary>Removes a task, to be deleted on the next commit.</summary>
    void Remove(TaskItem taskItem);
}

namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// A collection of <see cref="TaskItem"/> aggregates.
/// </summary>
/// <remarks>
/// There is deliberately no <c>Update</c> method — EF Core's change tracker records mutations made
/// through the aggregate's own methods automatically — and no <c>SaveChangesAsync</c>: committing a
/// unit of work is a transaction-boundary concern that belongs to an <c>IUnitOfWork</c> interface in
/// the Application layer, not here. A repository is a collection of aggregates, describable without
/// ever saying "database"; a unit of work is a transaction boundary, which cannot be described
/// without saying "commit" — the two are different responsibilities, and this interface owns only
/// the first.
/// </remarks>
public interface ITaskItemRepository
{
    /// <summary>Finds a task by its id.</summary>
    /// <param name="id">The id to look up.</param>
    /// <param name="cancellationToken">A token to cancel the lookup.</param>
    /// <returns>The matching task, or <see langword="null"/> when no task has that id.</returns>
    Task<TaskItem?> GetByIdAsync(TaskItemId id, CancellationToken cancellationToken = default);

    /// <summary>Lists tasks, optionally filtered by status, one page at a time.</summary>
    /// <param name="status">An optional filter: when not <see langword="null"/>, only tasks with this status are returned.</param>
    /// <param name="skip">The number of matching tasks to skip, for paging.</param>
    /// <param name="take">The maximum number of tasks to return.</param>
    /// <param name="cancellationToken">A token to cancel the query.</param>
    /// <returns>The matching page of tasks.</returns>
    Task<IReadOnlyList<TaskItem>> ListAsync(TaskItemStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Marks <paramref name="taskItem"/> to be inserted on the next commit.</summary>
    /// <param name="taskItem">The task to add.</param>
    void Add(TaskItem taskItem);

    /// <summary>Marks <paramref name="taskItem"/> to be hard-deleted on the next commit.</summary>
    /// <param name="taskItem">The task to remove.</param>
    void Remove(TaskItem taskItem);
}

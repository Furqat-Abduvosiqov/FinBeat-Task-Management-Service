using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks.Events;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// The Tasks aggregate root: a single unit of work a user tracks from creation through either
/// completion/archival or hard deletion.
/// </summary>
/// <remarks>
/// For whoever writes the Infrastructure layer: the <c>SaveChangesAsync</c> interceptor that drains
/// <see cref="AggregateRoot{TId}.DomainEvents"/> must read the change tracker's entries
/// <em>before</em> the save completes. A hard-deleted <see cref="TaskItem"/> is detached from the
/// change tracker once <c>SaveChangesAsync</c> returns, so collecting events afterwards would let
/// its <see cref="TaskItemDeletedDomainEvent"/> silently vanish — breaking the requirement that a
/// listener service receives deletion events.
/// </remarks>
public sealed class TaskItem : AggregateRoot<TaskItemId>
{
    /// <summary>Reserved for EF Core materialization; the ORM populates every property by reflection afterwards.</summary>
    private TaskItem()
    {
    }

    private TaskItem(TaskItemId id, TaskTitle title, TaskDescription description, DateTimeOffset now)
        : base(id)
    {
        Title = title;
        Description = description;
        Status = TaskItemStatus.New;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>The task's title.</summary>
    public TaskTitle Title { get; private set; } = null!;

    /// <summary>The task's description. Never null; an absent description is represented by <see cref="TaskDescription.None"/>.</summary>
    public TaskDescription Description { get; private set; } = null!;

    /// <summary>The task's current lifecycle status.</summary>
    public TaskItemStatus Status { get; private set; }

    /// <summary>The instant, in UTC, at which the task was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The instant, in UTC, at which the task was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates a new task with status <see cref="TaskItemStatus.New"/>.</summary>
    /// <param name="title">The task's title.</param>
    /// <param name="description">The task's description.</param>
    /// <param name="clock">Supplies the id's embedded timestamp and the initial <see cref="CreatedAt"/>/<see cref="UpdatedAt"/>.</param>
    /// <returns>The newly created task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="title"/>, <paramref name="description"/>, or <paramref name="clock"/> is <see langword="null"/>.</exception>
    public static TaskItem Create(TaskTitle title, TaskDescription description, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(clock);

        // A single call to the clock, so CreatedAt and UpdatedAt are identical, not merely close.
        var now = clock.GetUtcNow();
        var task = new TaskItem(TaskItemId.New(clock), title, description, now);

        task.Raise(new TaskItemCreatedDomainEvent(task.Id, task.Title, task.Description, task.Status, now));

        return task;
    }

    /// <summary>Updates the task's title and description.</summary>
    /// <param name="title">The new title.</param>
    /// <param name="description">The new description.</param>
    /// <param name="clock">Supplies the new <see cref="UpdatedAt"/> when the update is not a no-op.</param>
    /// <remarks>
    /// If both <paramref name="title"/> and <paramref name="description"/> already equal the
    /// current values, this is a no-op: <see cref="UpdatedAt"/> is not bumped and no event is
    /// raised, so re-submitting unchanged details does not pollute the audit trail.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="title"/>, <paramref name="description"/>, or <paramref name="clock"/> is <see langword="null"/>.</exception>
    public void UpdateDetails(TaskTitle title, TaskDescription description, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(clock);

        if (Title == title && Description == description)
        {
            return;
        }

        Title = title;
        Description = description;
        UpdatedAt = clock.GetUtcNow();

        Raise(new TaskItemDetailsUpdatedDomainEvent(Id, Title, Description, UpdatedAt));
    }

    /// <summary>Moves the task to <paramref name="newStatus"/>.</summary>
    /// <param name="newStatus">The status to move to.</param>
    /// <param name="clock">Supplies the new <see cref="UpdatedAt"/> when the change is not a no-op.</param>
    /// <remarks>
    /// If <paramref name="newStatus"/> already equals <see cref="Status"/>, this is a no-op: no
    /// exception, no event, and <see cref="UpdatedAt"/> is left untouched, which makes the
    /// operation idempotent and stops a downstream listener from logging phantom changes. There is
    /// deliberately no separate <c>Archive()</c> or <c>Restore()</c> method — archiving and
    /// restoring are themselves status transitions, so this is the single way a task moves between
    /// states.
    /// </remarks>
    /// <exception cref="InvalidTaskStatusTransitionException">
    /// The move from <see cref="Status"/> to <paramref name="newStatus"/> is not allowed by
    /// <see cref="TaskItemStatusRules.CanTransition"/>.
    /// </exception>
    public void ChangeStatus(TaskItemStatus newStatus, TimeProvider clock)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!TaskItemStatusRules.CanTransition(Status, newStatus))
        {
            throw new InvalidTaskStatusTransitionException(Status, newStatus);
        }

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = clock.GetUtcNow();

        Raise(new TaskItemStatusChangedDomainEvent(Id, previousStatus, Status, UpdatedAt));
    }

    /// <summary>Records that the task is being deleted.</summary>
    /// <param name="clock">Supplies the deletion event's timestamp.</param>
    /// <remarks>
    /// Deletion is permitted from any status, including <see cref="TaskItemStatus.Archived"/>. This
    /// method only records the intent, by raising <see cref="TaskItemDeletedDomainEvent"/>; the
    /// Application layer follows with <c>ITaskItemRepository.Remove</c> and a commit. There is
    /// deliberately no deleted flag or timestamp on this aggregate — this project uses hard delete,
    /// and <see cref="TaskItemStatus.Archived"/> is the retention mechanism that replaces soft
    /// delete.
    /// </remarks>
    public void Delete(TimeProvider clock)
    {
        Raise(new TaskItemDeletedDomainEvent(Id, clock.GetUtcNow()));
    }
}

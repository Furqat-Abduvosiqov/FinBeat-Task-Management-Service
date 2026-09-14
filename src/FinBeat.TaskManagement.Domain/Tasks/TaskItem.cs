using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks.Events;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>A task a user tracks, from creation through to completion, archival or deletion.</summary>
/// <remarks>
/// Note for the Infrastructure layer: whatever drains <see cref="AggregateRoot{TId}.DomainEvents"/>
/// has to read the change tracker <em>before</em> the save completes. EF detaches a deleted entity
/// afterwards, so a later read loses its deletion event and the listener never hears about it.
/// </remarks>
public sealed class TaskItem : AggregateRoot<TaskItemId>
{
    /// <summary>Reserved for EF Core, which sets the properties by reflection afterwards.</summary>
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

    /// <summary>What the task is called.</summary>
    public TaskTitle Title { get; private set; } = null!;

    /// <summary>What the task involves. Never null — an absent one is <see cref="TaskDescription.None"/>.</summary>
    public TaskDescription Description { get; private set; } = null!;

    /// <summary>Where the task is in its lifecycle.</summary>
    public TaskItemStatus Status { get; private set; }

    /// <summary>When the task was created, in UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the task last changed, in UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates a task, ready to be added to a repository.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static TaskItem Create(TaskTitle title, TaskDescription description, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(clock);

        // One read of the clock, so CreatedAt and UpdatedAt are identical rather than merely close.
        var now = clock.GetUtcNow();
        var task = new TaskItem(TaskItemId.New(), title, description, now);

        task.Raise(new TaskItemCreatedDomainEvent(task.Id, task.Title, task.Description, task.Status, now));

        return task;
    }

    /// <summary>Changes the title and description.</summary>
    /// <remarks>Submitting the values it already has does nothing, so the history stays honest.</remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
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

    /// <summary>Moves the task to another status. Archiving and restoring go through here too.</summary>
    /// <remarks>
    /// Setting the status it already has does nothing, which keeps the call idempotent and stops
    /// the listener logging changes that never happened.
    /// </remarks>
    /// <exception cref="InvalidTaskStatusTransitionException">The move is not allowed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    public void ChangeStatus(TaskItemStatus newStatus, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

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

    /// <summary>Records that the task is being deleted. Allowed from any status.</summary>
    /// <remarks>
    /// Only records the intent — the repository removes the row. Nothing is flagged on the
    /// aggregate because deletion here is real; <see cref="TaskItemStatus.Archived"/> is what you
    /// use to keep a task instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    public void Delete(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        Raise(new TaskItemDeletedDomainEvent(Id, clock.GetUtcNow()));
    }
}

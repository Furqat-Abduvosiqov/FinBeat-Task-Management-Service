using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>A task moved from one status to another.</summary>
/// <remarks>
/// Carries the previous status as well as the new one, which is what lets archiving be the only
/// retention mechanism: this event is the record of what a task was before it was archived, even
/// though restoring always returns it to <see cref="TaskItemStatus.New"/>.
/// </remarks>
public sealed record TaskItemStatusChangedDomainEvent(
    TaskItemId TaskId,
    TaskItemStatus PreviousStatus,
    TaskItemStatus CurrentStatus,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>A task moved from one status to another.</summary>
/// <remarks>Carries the previous status too, so the event records what a task was before archiving.</remarks>
public sealed record TaskItemStatusChangedDomainEvent(
    TaskItemId TaskId,
    TaskItemStatus PreviousStatus,
    TaskItemStatus CurrentStatus,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

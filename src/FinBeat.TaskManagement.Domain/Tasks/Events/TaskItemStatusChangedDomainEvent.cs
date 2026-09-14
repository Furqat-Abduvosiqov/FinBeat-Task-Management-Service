using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>Raised when a <c>TaskItem</c> moves from one status to another.</summary>
/// <param name="TaskId">The id of the task whose status changed.</param>
/// <param name="PreviousStatus">The status the task held immediately before this change.</param>
/// <param name="CurrentStatus">The status the task holds after this change.</param>
/// <param name="OccurredOnUtc">The instant, in UTC, at which the status changed.</param>
/// <remarks>
/// Domain events never touch the wire. The Application layer maps every domain event to a flat,
/// primitives-only integration contract, and only that contract is serialized, for the outbox or a
/// message broker. Carrying both <paramref name="PreviousStatus"/> and
/// <paramref name="CurrentStatus"/> lets a downstream listener log the transition without a
/// round-trip to the database, and it is also what makes archiving safe as the sole retention
/// mechanism: when a task is later archived, this event is the permanent record of the status
/// (e.g. <see cref="TaskItemStatus.Completed"/>) it held immediately beforehand, even though
/// <c>TaskItem.ChangeStatus</c> always restores an archived task to <see cref="TaskItemStatus.New"/>.
/// </remarks>
public sealed record TaskItemStatusChangedDomainEvent(
    TaskItemId TaskId,
    TaskItemStatus PreviousStatus,
    TaskItemStatus CurrentStatus,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>Raised when a <c>TaskItem</c> is deleted.</summary>
/// <param name="TaskId">The id of the task that was deleted.</param>
/// <param name="OccurredOnUtc">The instant, in UTC, at which the deletion happened.</param>
/// <remarks>
/// Domain events never touch the wire. The Application layer maps every domain event to a flat,
/// primitives-only integration contract, and only that contract is serialized, for the outbox or a
/// message broker. This event carries nothing beyond <paramref name="TaskId"/> and the timestamp,
/// but that is still enough to reach a listener without a round-trip to the database — after a hard
/// delete there is no row left to re-read, so the id is deliberately all a listener needs to record
/// that the task is gone.
/// </remarks>
public sealed record TaskItemDeletedDomainEvent(
    TaskItemId TaskId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

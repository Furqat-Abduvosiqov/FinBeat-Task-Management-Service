using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>Raised when a new <c>TaskItem</c> is created.</summary>
/// <param name="TaskId">The id of the task that was created.</param>
/// <param name="Title">The task's initial title.</param>
/// <param name="Description">The task's initial description.</param>
/// <param name="Status">The task's initial status, always <see cref="TaskItemStatus.New"/>.</param>
/// <param name="OccurredOnUtc">The instant, in UTC, at which the task was created.</param>
/// <remarks>
/// Domain events never touch the wire. The Application layer maps every domain event to a flat,
/// primitives-only integration contract, and only that contract is serialized, for the outbox or a
/// message broker. This matters concretely for this event: <see cref="Tasks.TaskTitle"/> has a
/// private constructor and a get-only property, so <c>System.Text.Json</c> can serialize it but
/// cannot deserialize it back — a failure that would only surface later, when a publisher reads an
/// outbox row and tries to rehydrate this event from it. Carrying the full payload here (rather
/// than just <paramref name="TaskId"/>) lets a downstream listener log what was created without a
/// round-trip to the database.
/// </remarks>
public sealed record TaskItemCreatedDomainEvent(
    TaskItemId TaskId,
    TaskTitle Title,
    TaskDescription Description,
    TaskItemStatus Status,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

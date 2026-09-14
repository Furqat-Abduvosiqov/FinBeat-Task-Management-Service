using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>A task was created.</summary>
/// <remarks>Carries the payload, not just the id, so a listener can log it without a database round-trip.</remarks>
public sealed record TaskItemCreatedDomainEvent(
    TaskItemId TaskId,
    TaskTitle Title,
    TaskDescription Description,
    TaskItemStatus Status,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

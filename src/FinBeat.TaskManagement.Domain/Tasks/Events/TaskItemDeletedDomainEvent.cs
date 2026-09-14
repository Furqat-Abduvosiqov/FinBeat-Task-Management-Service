using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>A task was deleted.</summary>
/// <remarks>Only the id — after a hard delete there is no row left to read anything else from.</remarks>
public sealed record TaskItemDeletedDomainEvent(
    TaskItemId TaskId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

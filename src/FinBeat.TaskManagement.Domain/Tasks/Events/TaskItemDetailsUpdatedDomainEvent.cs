using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Events;

/// <summary>A task's title or description changed.</summary>
public sealed record TaskItemDetailsUpdatedDomainEvent(
    TaskItemId TaskId,
    TaskTitle Title,
    TaskDescription Description,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

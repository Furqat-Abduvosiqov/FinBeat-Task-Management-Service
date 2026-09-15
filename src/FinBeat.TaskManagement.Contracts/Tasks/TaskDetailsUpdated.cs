namespace FinBeat.TaskManagement.Contracts.Tasks;

/// <summary>Published when a task title or description changes.</summary>
/// <param name="TaskId">The task that changed.</param>
/// <param name="Title">The new title.</param>
/// <param name="Description">The new description, empty when there is none.</param>
/// <param name="OccurredOnUtc">When the change happened, in UTC.</param>
public sealed record TaskDetailsUpdated(
    Guid TaskId,
    string Title,
    string Description,
    DateTimeOffset OccurredOnUtc);

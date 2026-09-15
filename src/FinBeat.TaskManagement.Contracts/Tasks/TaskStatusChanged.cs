namespace FinBeat.TaskManagement.Contracts.Tasks;

/// <summary>Published when a task moves from one status to another.</summary>
/// <param name="TaskId">The task that changed.</param>
/// <param name="PreviousStatus">The status the task was in.</param>
/// <param name="CurrentStatus">The status it moved to.</param>
/// <param name="OccurredOnUtc">When the change happened, in UTC.</param>
/// <remarks>Status as a name, not a number: a name survives a renumbering consumers never hear about.</remarks>
public sealed record TaskStatusChanged(
    Guid TaskId,
    string PreviousStatus,
    string CurrentStatus,
    DateTimeOffset OccurredOnUtc);

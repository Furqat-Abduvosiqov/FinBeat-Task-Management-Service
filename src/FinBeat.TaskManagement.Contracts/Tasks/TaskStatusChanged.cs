namespace FinBeat.TaskManagement.Contracts.Tasks;

/// <summary>Published when a task moves from one status to another.</summary>
/// <param name="TaskId">The task that changed.</param>
/// <param name="PreviousStatus">The status the task was in.</param>
/// <param name="CurrentStatus">The status it moved to.</param>
/// <param name="OccurredOnUtc">When the change happened, in UTC.</param>
/// <remarks>
/// Status names rather than their numbers: the wire format is read by other services and by people
/// reading logs, and a name survives a renumbering that a consumer never hears about.
/// </remarks>
public sealed record TaskStatusChanged(
    Guid TaskId,
    string PreviousStatus,
    string CurrentStatus,
    DateTimeOffset OccurredOnUtc);

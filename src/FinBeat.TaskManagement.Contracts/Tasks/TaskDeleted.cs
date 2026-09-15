namespace FinBeat.TaskManagement.Contracts.Tasks;

/// <summary>Published when a task is deleted.</summary>
/// <param name="TaskId">The task that was deleted.</param>
/// <param name="OccurredOnUtc">When the deletion happened, in UTC.</param>
/// <remarks>Only the id: after a hard delete there is no row left to read anything else from.</remarks>
public sealed record TaskDeleted(Guid TaskId, DateTimeOffset OccurredOnUtc);

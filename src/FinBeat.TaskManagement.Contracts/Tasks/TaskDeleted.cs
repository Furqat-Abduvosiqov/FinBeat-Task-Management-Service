namespace FinBeat.TaskManagement.Contracts.Tasks;

/// <summary>Published when a task is deleted.</summary>
/// <param name="TaskId">The task that was deleted.</param>
/// <param name="OccurredOnUtc">When the deletion happened, in UTC.</param>
/// <remarks>Only the id, deliberately: the aggregate is still tracked when this is built, so the title was in hand - but a deletion notice that carried it would invite consumers to depend on state that no longer exists.</remarks>
public sealed record TaskDeleted(Guid TaskId, DateTimeOffset OccurredOnUtc);

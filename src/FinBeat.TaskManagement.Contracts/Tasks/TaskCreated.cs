namespace FinBeat.TaskManagement.Contracts.Tasks;

/// <summary>Published when a task is created.</summary>
/// <param name="TaskId">The task that was created.</param>
/// <param name="Title">The task title.</param>
/// <param name="Description">The task description, empty when there is none.</param>
/// <param name="Status">The status name, for example <c>New</c>.</param>
/// <param name="OccurredOnUtc">When the change happened, in UTC.</param>
/// <remarks>Primitives only: domain events carry value objects that cannot be deserialized back.</remarks>
public sealed record TaskCreated(
    Guid TaskId,
    string Title,
    string Description,
    string Status,
    DateTimeOffset OccurredOnUtc);

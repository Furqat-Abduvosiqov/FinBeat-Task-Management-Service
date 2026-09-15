using FinBeat.TaskManagement.Domain.Tasks;

namespace FinBeat.TaskManagement.Application.Tasks;

/// <summary>A task, as everything outside the domain sees it.</summary>
/// <param name="Id">The task id.</param>
/// <param name="Title">The task title.</param>
/// <param name="Description">The description, empty when there is none.</param>
/// <param name="Status">The status, as its number. The OpenAPI document names each one.</param>
/// <param name="CreatedAt">When the task was created, in UTC.</param>
/// <param name="UpdatedAt">When the task last changed, in UTC.</param>
/// <remarks>Primitives only, for the reason the integration contracts are: value objects with private constructors serialize but never come back.</remarks>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    internal static TaskResponse From(TaskItem task) => new(
        task.Id.Value,
        task.Title.Value,
        task.Description.Value,
        task.Status,
        task.CreatedAt,
        task.UpdatedAt);
}

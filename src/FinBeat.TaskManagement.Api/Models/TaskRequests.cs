using FinBeat.TaskManagement.Domain.Tasks;

namespace FinBeat.TaskManagement.Api.Endpoints;

/// <summary>The body of a create-task request.</summary>
/// <param name="Title">What the task is called. Required, trimmed, at most 200 characters.</param>
/// <param name="Description">What the task involves. Optional: null or whitespace means none.</param>
public sealed record CreateTaskRequest(string? Title, string? Description);

/// <summary>The body of an update-details request.</summary>
/// <param name="Title">The new title. Required, trimmed, at most 200 characters.</param>
/// <param name="Description">The new description. Optional: null or whitespace clears it.</param>
/// <remarks>Both values are replaced, so omitting the description clears it rather than leaving it alone.</remarks>
public sealed record UpdateTaskDetailsRequest(string? Title, string? Description);

/// <summary>The body of a status-change request.</summary>
/// <param name="Status">The status to move the task to, as its number. The OpenAPI document names each one.</param>
public sealed record ChangeTaskStatusRequest(TaskItemStatus Status);

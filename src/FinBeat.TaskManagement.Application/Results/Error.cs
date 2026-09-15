namespace FinBeat.TaskManagement.Application.Results;

/// <summary>Why an operation failed.</summary>
/// <param name="Code">A stable identifier such as <c>task.not-found</c>. Unlike the description it is safe to match on.</param>
/// <param name="Description">A human-readable explanation.</param>
/// <param name="Type">The kind of failure.</param>
public sealed record Error(string Code, string Description, ErrorType Type);

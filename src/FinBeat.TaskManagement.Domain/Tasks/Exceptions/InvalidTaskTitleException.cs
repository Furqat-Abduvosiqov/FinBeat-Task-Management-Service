using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>A title was empty, whitespace-only, or too long.</summary>
/// <param name="message">Why the title was rejected.</param>
public sealed class InvalidTaskTitleException(string message) : DomainException(message)
{
    /// <inheritdoc />
    public override string ErrorCode => "task.title.invalid";
}

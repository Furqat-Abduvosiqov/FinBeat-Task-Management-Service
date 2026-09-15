using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>A description was too long.</summary>
/// <param name="message">Why the description was rejected.</param>
public sealed class InvalidTaskDescriptionException(string message) : DomainException(message)
{
    /// <inheritdoc />
    public override string ErrorCode => "task.description.invalid";
}

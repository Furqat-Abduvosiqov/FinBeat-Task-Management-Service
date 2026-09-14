using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>
/// Raised by <see cref="TaskDescription.Create"/> when a candidate description exceeds
/// <see cref="TaskDescription.MaxLength"/> characters.
/// </summary>
public sealed class InvalidTaskDescriptionException : DomainException
{
    /// <summary>Initializes a new instance with a message describing why the description was rejected.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public InvalidTaskDescriptionException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "task.description.invalid";
}

using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>
/// Raised by <see cref="TaskTitle.Create"/> when a candidate title is null, empty,
/// whitespace-only, or exceeds <see cref="TaskTitle.MaxLength"/> characters.
/// </summary>
public sealed class InvalidTaskTitleException : DomainException
{
    /// <summary>Initializes a new instance with a message describing why the title was rejected.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public InvalidTaskTitleException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "task.title.invalid";
}

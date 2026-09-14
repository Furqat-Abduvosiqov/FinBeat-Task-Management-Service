using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>A title was empty, whitespace-only, or too long.</summary>
public sealed class InvalidTaskTitleException : DomainException
{
    /// <summary>Creates the exception with a message describing why the title was rejected.</summary>
    public InvalidTaskTitleException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "task.title.invalid";
}

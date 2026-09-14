using FinBeat.TaskManagement.Domain.Abstractions;

namespace FinBeat.TaskManagement.Domain.Tasks.Exceptions;

/// <summary>A description was too long.</summary>
public sealed class InvalidTaskDescriptionException : DomainException
{
    /// <summary>Creates the exception with a message describing why the description was rejected.</summary>
    public InvalidTaskDescriptionException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "task.description.invalid";
}

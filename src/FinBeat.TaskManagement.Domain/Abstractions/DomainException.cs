namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>
/// Base type for exceptions raised when the domain rejects an operation because it would violate
/// one of its invariants.
/// </summary>
/// <remarks>
/// <see cref="ErrorCode"/> is a stable, machine-readable identifier, independent of the
/// human-readable and potentially reworded <see cref="Exception.Message"/>, that the API layer
/// later maps to an RFC 7807 <c>ProblemDetails</c> <c>type</c> URI. Codes are dotted and
/// lowercase, e.g. <c>task.title.invalid</c>.
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>Initializes a new instance with a message describing the violated invariant.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and the exception that caused it.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>A stable, dotted, lowercase, machine-readable identifier for this failure, e.g. <c>task.title.invalid</c>.</summary>
    public abstract string ErrorCode { get; }
}

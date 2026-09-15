namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>Raised when an operation would break one of the domain's invariants.</summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates the exception with a message describing the broken rule.</summary>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// A stable identifier for this failure, such as <c>task.title.invalid</c>. Unlike the message
    /// it is safe to match on, and the API sends it as the problem-details <c>code</c> member.
    /// </summary>
    public abstract string ErrorCode { get; }
}

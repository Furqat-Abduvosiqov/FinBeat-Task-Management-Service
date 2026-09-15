namespace FinBeat.TaskManagement.Application.Results;

/// <summary>What kind of failure an <see cref="Error"/> describes.</summary>
/// <remarks>A host maps this to a status code, so it never has to match on error codes it would then have to keep in step.</remarks>
public enum ErrorType
{
    /// <summary>The request itself was not acceptable.</summary>
    Validation = 1,

    /// <summary>Nothing exists with the identity that was asked for.</summary>
    NotFound = 2,

    /// <summary>The request contradicts the current state.</summary>
    Conflict = 3,
}

using FinBeat.TaskManagement.Application.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FinBeat.TaskManagement.Api.Endpoints;

internal static class ErrorResults
{
    /// <summary>Renders a use-case failure as problem details.</summary>
    /// <remarks>
    /// The status comes from <see cref="ErrorType"/> rather than from the error code, so adding a
    /// code never means revisiting this. <see cref="Error.Code"/> travels as the <c>code</c>
    /// extension, since that is the part callers are told they may match on. The title is left to
    /// the framework, which fills it from the status along with the RFC 9110 type URI.
    /// </remarks>
    internal static ProblemHttpResult ToProblem(this Error error) => TypedResults.Problem(
        statusCode: StatusCodeFor(error.Type),
        detail: error.Description,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}

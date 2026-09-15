using FinBeat.TaskManagement.Application.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FinBeat.TaskManagement.Api.Endpoints;

internal static class ErrorResults
{
    /// <summary>The problem-details member carrying the stable error identifier callers may match on.</summary>
    /// <remarks>One definition, because the promise is that every failure uses the same name - half of them under a typo would be worse than none.</remarks>
    internal const string CodeMember = "code";

    /// <summary>Renders a use-case failure as problem details.</summary>
    /// <remarks>Status comes from <see cref="ErrorType"/>; the error code travels as a <c>code</c> extension.</remarks>
    internal static ProblemHttpResult ToProblem(this Error error) => TypedResults.Problem(
        statusCode: StatusCodeFor(error.Type),
        detail: error.Description,
        extensions: new Dictionary<string, object?> { [CodeMember] = error.Code });

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}

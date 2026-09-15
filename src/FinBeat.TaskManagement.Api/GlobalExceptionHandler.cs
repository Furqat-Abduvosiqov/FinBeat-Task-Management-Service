using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FinBeat.TaskManagement.Api;

/// <summary>Turns an unhandled exception into RFC 9457 problem details: the framework's own status for a request it could not read, 500 for anything else.</summary>
/// <remarks>The body never names what actually broke - that belongs in the log. <c>CustomizeProblemDetails</c> adds the <c>traceId</c> that joins the two, on every problem response.</remarks>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A body the model binder could not read is the caller's mistake, and the framework has
        // already decided which 4xx says so. Everything else is ours, and is a 500.
        var malformedRequest = exception as BadHttpRequestException;
        var statusCode = malformedRequest?.StatusCode ?? StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Detail = malformedRequest is null
                    ? "An unexpected error occurred while processing the request."
                    : "The request could not be read.",
            }
        });
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FinBeat.TaskManagement.Api;

/// <summary>Turns any unhandled exception into a 500 carrying RFC 9457 problem details.</summary>
/// <remarks>The body is the same for every exception: what went wrong belongs in the log. <c>traceId</c>, the handle that joins the two, is added by <c>ProblemDetailsOptions.CustomizeProblemDetails</c> for every problem response, not only this one.</remarks>
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

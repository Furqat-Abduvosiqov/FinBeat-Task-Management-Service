using FluentValidation;

namespace FinBeat.TaskManagement.Api.Endpoints.Validation;

/// <summary>Validates the request body before the endpoint runs.</summary>
/// <typeparam name="TRequest">The bound request type to validate.</typeparam>
/// <param name="validator">The rules for that type.</param>
/// <remarks>A filter rather than a check inside each handler, so adding an endpoint is one line and a request type cannot quietly go unvalidated.</remarks>
internal sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    /// <summary>The <c>code</c> a rejected request carries, alongside the per-field errors.</summary>
    internal const string ErrorCode = "request.invalid";

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (result.IsValid)
        {
            return await next(context);
        }

        // The same code extension every other failure carries, so a caller matching on it does not
        // need a second branch for the validated ones.
        return TypedResults.ValidationProblem(
            result.ToDictionary(),
            extensions: new Dictionary<string, object?> { ["code"] = ErrorCode });
    }
}

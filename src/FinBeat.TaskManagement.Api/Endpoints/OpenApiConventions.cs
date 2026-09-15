using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace FinBeat.TaskManagement.Api.Endpoints;

internal static class OpenApiConventions
{
    /// <summary>A task status as it travels: the name, with the legal values listed.</summary>
    /// <remarks>Stated rather than inferred, because Swashbuckle reads schemas with its own serializer and would otherwise render an integer where the wire carries a name.</remarks>
    internal static OpenApiSchema StatusSchema() => new()
    {
        Type = "string",
        Enum = Enum.GetNames<TaskItemStatus>().Select(name => (IOpenApiAny)new OpenApiString(name)).ToList(),
    };

    /// <summary>Documents one parameter of an operation, if it has one by that name.</summary>
    /// <param name="operation">The operation being described.</param>
    /// <param name="parameterName">The parameter to describe.</param>
    /// <param name="description">What to say about it.</param>
    internal static void Describe(OpenApiOperation operation, string parameterName, string description)
    {
        var parameter = operation.Parameters
            .FirstOrDefault(candidate => string.Equals(candidate.Name, parameterName, StringComparison.Ordinal));

        if (parameter is not null)
        {
            parameter.Description = description;
        }
    }
}

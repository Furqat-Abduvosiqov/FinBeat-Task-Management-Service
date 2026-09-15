using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinBeat.TaskManagement.Api.OpenApi;

/// <summary>States the schema of every enum query parameter, so its values are documented.</summary>
/// <remarks>
/// A filter rather than <c>MapType</c> or a <c>WithOpenApi</c> transform: Swashbuckle generates
/// parameter schemas itself after merging endpoint metadata, carrying only the description across, so
/// anything set earlier is overwritten. Filters run last.
///
/// A parameter also never reaches the named component a request body would <c>$ref</c>, which is why
/// the same description has to be written here as well as by <see cref="EnumSchemaFilter"/>.
/// </remarks>
internal sealed class EnumParameterFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var described in context.ApiDescription.ParameterDescriptions)
        {
            var enumType = EnumTypeOf(described);

            if (enumType is null)
            {
                continue;
            }

            var parameter = operation.Parameters?
                .FirstOrDefault(candidate => string.Equals(candidate.Name, described.Name, StringComparison.Ordinal));

            if (parameter is not null)
            {
                parameter.Schema = EnumDocumentation.Schema(enumType);
            }
        }
    }

    private static Type? EnumTypeOf(ApiParameterDescription described)
    {
        var type = Nullable.GetUnderlyingType(described.Type) ?? described.Type;

        return type.IsEnum ? type : null;
    }
}

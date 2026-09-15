using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinBeat.TaskManagement.Api.OpenApi;

/// <summary>Adds each enum member's number, name and meaning to the schema request bodies point at.</summary>
/// <remarks>Applies to every enum rather than one of them, so a second enum is documented without a second filter.</remarks>
internal sealed class EnumSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            // Swashbuckle has already written the type's own summary here, which is worth keeping.
            schema.Description = EnumDocumentation.Describe(context.Type, schema.Description);
        }
    }
}

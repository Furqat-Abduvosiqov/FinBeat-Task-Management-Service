using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinBeat.TaskManagement.Api.OpenApi;

/// <summary>States the schema of every task-status parameter, so the legal names are documented.</summary>
/// <remarks>
/// A filter rather than <c>MapType</c> or a <c>WithOpenApi</c> transform: Swashbuckle generates
/// parameter schemas itself after merging endpoint metadata, carrying only the description across, so
/// anything set earlier is overwritten. Filters run last.
/// </remarks>
internal sealed class StatusParameterFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var described in context.ApiDescription.ParameterDescriptions.Where(IsStatus))
        {
            var parameter = operation.Parameters?
                .FirstOrDefault(candidate => string.Equals(candidate.Name, described.Name, StringComparison.Ordinal));

            if (parameter is not null)
            {
                parameter.Schema = OpenApiConventions.StatusSchema();
            }
        }
    }

    private static bool IsStatus(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription described) =>
        described.Type == typeof(TaskItemStatus)
        || Nullable.GetUnderlyingType(described.Type) == typeof(TaskItemStatus);
}

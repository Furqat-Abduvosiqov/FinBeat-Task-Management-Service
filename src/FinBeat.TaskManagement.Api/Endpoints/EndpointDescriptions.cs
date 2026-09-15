using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.OpenApi.Models;

namespace FinBeat.TaskManagement.Api.Endpoints;

/// <summary>Builds the prose the OpenAPI document publishes about the task endpoints.</summary>
/// <remarks>Kept apart from the routes: describing an operation and serving it are different jobs, and only one changes when a rule does.</remarks>
internal static class EndpointDescriptions
{
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

    // Reads the transition matrix off TaskItemStatusRules instead of restating it in prose, so the
    // published description cannot drift from rules the code has since changed.
    internal static string DescribeTransitions()
    {
        var statuses = Enum.GetValues<TaskItemStatus>();

        var moves = statuses
            .Select(from => (From: from, To: statuses.Where(to => TaskItemStatusRules.CanTransition(from, to))))
            .Where(move => move.To.Any())
            .Select(move => $"{move.From} to {JoinWithOr(move.To)}");

        return string.Join("; ", moves);
    }

    // Joins the destinations of one move the way the sentence reads them: "A, B or C".
    private static string JoinWithOr(IEnumerable<TaskItemStatus> destinations)
    {
        var names = destinations.Select(status => status.ToString()).ToArray();

        return names.Length == 1
            ? names[0]
            : string.Join(", ", names[..^1]) + " or " + names[^1];
    }
}

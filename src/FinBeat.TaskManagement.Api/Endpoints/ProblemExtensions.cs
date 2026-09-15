namespace FinBeat.TaskManagement.Api.Endpoints;

/// <summary>The problem-details extensions this API adds beyond RFC 9457.</summary>
internal static class ProblemExtensions
{
    /// <summary>Carries the stable error identifier callers are told they may match on.</summary>
    /// <remarks>One definition, because the promise is that every failure uses the same name - half of them under a typo would be worse than none.</remarks>
    internal const string Code = "code";
}

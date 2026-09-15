namespace FinBeat.TaskManagement.IntegrationTests;

/// <summary>The container images the tests start.</summary>
/// <remarks>
/// Named here rather than at each call site so two fixtures cannot drift onto different tags, and so
/// <c>ContainerImageTests</c> has something to compare against what the repository declares elsewhere.
/// Every one of these has to match docker-compose.yml: a test that runs against a different build of
/// a dependency than the stack does is testing something nobody ships, and pulls a second copy to do it.
/// </remarks>
internal static class TestImages
{
    /// <summary>The database behind the container-backed suites.</summary>
    public const string PostgreSql = "postgres:16-alpine";

    /// <summary>The broker. The management variant, because that is what the stack runs.</summary>
    public const string RabbitMq = "rabbitmq:3-management-alpine";

    /// <summary>Every image a test starts.</summary>
    public static readonly string[] All = [PostgreSql, RabbitMq];
}

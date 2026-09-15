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
    public const string PostgreSql = "postgres:16-alpine@sha256:cf78e76683b9ca8c5733cbbdce6c9262b45b6767934dd0a95e671f9a0fc20685";

    /// <summary>The broker. The management variant, because that is what the stack runs.</summary>
    public const string RabbitMq = "rabbitmq:3-management-alpine@sha256:606d8c0d6b3c18d1da9afc53bc7cdb2a8d5486df91b5a9830e9e07626c9ae281";

    /// <summary>Every image a test starts.</summary>
    public static readonly string[] All = [PostgreSql, RabbitMq];
}

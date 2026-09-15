namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

/// <summary>
/// Groups every <c>RequiresDocker</c> test onto one shared <see cref="PostgresFixture"/>, so the
/// container starts and migrates exactly once per test run instead of once per test class.
/// </summary>
[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

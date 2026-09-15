namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

/// <summary>Groups every <c>RequiresDocker</c> test onto one shared <see cref="PostgresFixture"/>, so the container starts and migrates once per run.</summary>
[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

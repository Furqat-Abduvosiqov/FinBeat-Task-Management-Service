using Npgsql;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemConstraintTests
{
    private readonly PostgresFixture _fixture;

    public TaskItemConstraintTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Inserting_a_201_character_title_is_rejected_by_the_column_itself()
    {
        // Bypasses EF and TaskTitle entirely: this is what proves varchar(200) is a real column
        // constraint PostgreSQL enforces on its own, not only a model facet that only EF respects.
        await using var command = _fixture.DataSource.CreateCommand("""
            INSERT INTO tasks (id, title, description, status, created_at, updated_at)
            VALUES (@id, @title, @description, 'new'::task_item_status, @createdAt, @updatedAt)
            """);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("title", new string('a', 201));
        command.Parameters.AddWithValue("description", string.Empty);
        command.Parameters.AddWithValue("createdAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("updatedAt", DateTimeOffset.UtcNow);

        var exception = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        exception.SqlState.ShouldBe("22001");
    }
}

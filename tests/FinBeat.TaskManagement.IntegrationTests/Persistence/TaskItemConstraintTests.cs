using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Npgsql;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task A_title_wider_than_the_column_is_rejected_by_postgresql()
    {
        await using var command = Insert(title: new string('a', TaskTitle.MaxLength + 1), status: 1);

        var exception = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        // 22001 is string_data_right_truncation.
        exception.SqlState.ShouldBe("22001");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task A_status_outside_the_declared_range_is_rejected_by_the_check_constraint(int status)
    {
        // Both ends: 0 is below New and is what a default-initialised int would carry, 5 is above
        // Archived. A one-sided constraint such as "status > 0" passes the first and fails the second.
        await using var command = Insert(title: "Renew passport", status: status);

        var exception = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        // 23514 is check_violation.
        exception.SqlState.ShouldBe("23514");
        exception.ConstraintName.ShouldBe("ck_tasks_status");
    }

    // Bypasses EF and the domain: the point is that PostgreSQL enforces this itself. Note the two do
    // not measure length identically - string.Length counts UTF-16 units, varchar(200) counts
    // characters - so the domain is always at least as strict as the column, never the reverse.
    private NpgsqlCommand Insert(string title, int status)
    {
        var command = fixture.DataSource.CreateCommand("""
            INSERT INTO tasks (id, title, description, status, created_at, updated_at)
            VALUES (@id, @title, @description, @status, @createdAt, @updatedAt)
            """);

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("description", string.Empty);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("createdAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("updatedAt", DateTimeOffset.UtcNow);

        return command;
    }
}

using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Npgsql;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task A_title_longer_than_the_domain_allows_is_rejected_by_the_column_itself()
    {
        await using var command = Insert(title: new string('a', TaskTitle.MaxLength + 1), status: 1);

        var exception = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        // 22001 is string_data_right_truncation.
        exception.SqlState.ShouldBe("22001");
    }

    [Fact]
    public async Task A_status_outside_the_declared_range_is_rejected_by_the_check_constraint()
    {
        // 0 specifically: TaskItemStatus has no zero member, so this is the value a default-initialised int would produce.
        await using var command = Insert(title: "Renew passport", status: 0);

        var exception = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        // 23514 is check_violation.
        exception.SqlState.ShouldBe("23514");
        exception.ConstraintName.ShouldBe("ck_tasks_status");
    }

    // Bypasses EF and the domain entirely: the point is to prove PostgreSQL enforces this itself,
    // not just EF's model facets.
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

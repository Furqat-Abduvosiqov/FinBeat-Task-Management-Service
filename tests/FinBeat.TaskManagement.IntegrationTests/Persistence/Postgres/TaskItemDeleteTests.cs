using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemDeleteTests
{
    private readonly PostgresFixture _fixture;

    public TaskItemDeleteTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Removing_a_task_hard_deletes_the_row_instead_of_hiding_it_behind_a_query_filter()
    {
        var clock = Clock.New();
        var task = TaskItem.Create(TaskTitle.Create("Renew passport"), TaskDescription.None, clock);

        await using (var writeContext = _fixture.CreateContext())
        {
            writeContext.Tasks.Add(task);
            await writeContext.SaveChangesAsync();
        }

        await using (var deleteContext = _fixture.CreateContext())
        {
            var toDelete = await deleteContext.Tasks.SingleAsync(t => t.Id == task.Id);
            toDelete.Delete(clock);
            deleteContext.Tasks.Remove(toDelete);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleOrDefaultAsync(t => t.Id == task.Id);
        reloaded.ShouldBeNull();

        // The raw count is what tells a hard delete apart from a soft delete sitting behind a global
        // query filter - the EF query above would return null either way.
        await using var command = _fixture.DataSource.CreateCommand("SELECT count(*) FROM tasks WHERE id = @id");
        command.Parameters.AddWithValue("id", task.Id.Value);
        var count = (long)(await command.ExecuteScalarAsync())!;

        count.ShouldBe(0);
    }
}

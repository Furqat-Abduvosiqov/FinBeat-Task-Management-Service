using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemDeleteTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Removing_a_task_hard_deletes_the_row_instead_of_hiding_it_behind_a_query_filter()
    {
        var clock = Clock.New();
        var task = TaskItems.RenewPassport(clock);

        await fixture.SeedAsync(task);

        await using (var deleteContext = fixture.CreateContext())
        {
            var toDelete = await deleteContext.Tasks.SingleAsync(t => t.Id == task.Id);
            toDelete.Delete(clock);
            deleteContext.Tasks.Remove(toDelete);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleOrDefaultAsync(t => t.Id == task.Id);
        reloaded.ShouldBeNull();

        // The raw count is what tells a hard delete apart from a soft delete behind a query filter - the EF query above would return null either way.
        await using var command = fixture.DataSource.CreateCommand("SELECT count(*) FROM tasks WHERE id = @id");
        command.Parameters.AddWithValue("id", task.Id.Value);
        var count = (long)(await command.ExecuteScalarAsync())!;

        count.ShouldBe(0);
    }
}

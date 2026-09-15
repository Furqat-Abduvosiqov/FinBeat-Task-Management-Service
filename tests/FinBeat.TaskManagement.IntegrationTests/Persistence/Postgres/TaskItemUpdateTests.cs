using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemUpdateTests
{
    private readonly PostgresFixture _fixture;

    public TaskItemUpdateTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UpdateDetails_moves_UpdatedAt_and_leaves_CreatedAt_untouched_across_a_reload()
    {
        var clock = Clock.New();
        var task = TaskItem.Create(TaskTitle.Create("Renew passport"), TaskDescription.None, clock);

        await using (var writeContext = _fixture.CreateContext())
        {
            writeContext.Tasks.Add(task);
            await writeContext.SaveChangesAsync();
        }

        var createdAt = task.CreatedAt;
        var updatedAtBeforeUpdate = task.UpdatedAt;

        await using (var updateContext = _fixture.CreateContext())
        {
            var toUpdate = await updateContext.Tasks.SingleAsync(t => t.Id == task.Id);
            toUpdate.UpdateDetails(TaskTitle.Create("Renew passport urgently"), TaskDescription.None, clock);
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleAsync(t => t.Id == task.Id);

        reloaded.UpdatedAt.ShouldBeGreaterThan(updatedAtBeforeUpdate);
        reloaded.CreatedAt.ShouldBe(createdAt);
    }
}

using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemUpdateTests(PostgresFixture fixture)
{
    [Fact]
    public async Task UpdateDetails_moves_UpdatedAt_and_leaves_CreatedAt_untouched_across_a_reload()
    {
        var clock = Clock.New();
        var task = TaskItems.RenewPassport(clock);

        await fixture.SeedAsync(task);

        var createdAt = task.CreatedAt;
        var updatedAtBeforeUpdate = task.UpdatedAt;

        await using (var updateContext = fixture.CreateContext())
        {
            var toUpdate = await updateContext.Tasks.SingleAsync(t => t.Id == task.Id);
            toUpdate.UpdateDetails(TaskTitle.Create("Renew passport urgently"), TaskDescription.None, clock);
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleAsync(t => t.Id == task.Id);

        reloaded.UpdatedAt.ShouldBeGreaterThan(updatedAtBeforeUpdate);
        reloaded.CreatedAt.ShouldBe(createdAt);
    }
}

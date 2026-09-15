using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemRoundTripTests(PostgresFixture fixture)
{
    [Fact]
    public async Task A_saved_task_reloads_with_the_same_title_description_status_and_timestamps()
    {
        var clock = Clock.New();
        var task = TaskItems.RenewPassport(TaskDescription.Create("Before the trip in June"), clock);
        task.ChangeStatus(TaskItemStatus.InProgress, clock);

        await fixture.SeedAsync(task);

        // A fresh context per read, not ChangeTracker.Clear(): nothing here is served from the identity map.
        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleAsync(t => t.Id == task.Id);

        reloaded.Title.ShouldBe(task.Title);
        reloaded.Description.ShouldBe(task.Description);
        reloaded.Status.ShouldBe(TaskItemStatus.InProgress);
        reloaded.CreatedAt.ShouldBe(task.CreatedAt);
        reloaded.UpdatedAt.ShouldBe(task.UpdatedAt);
        reloaded.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task A_task_saved_with_no_description_reloads_as_the_None_singleton()
    {
        var clock = Clock.New();
        var task = TaskItems.RenewPassport(TaskDescription.None, clock);

        await fixture.SeedAsync(task);

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleAsync(t => t.Id == task.Id);

        // Reference identity, not value equality: fails on a null Description or if Create stopped
        // returning the shared None instance.
        reloaded.Description.ShouldBeSameAs(TaskDescription.None);
    }

    [Fact]
    public async Task Status_is_stored_as_the_int_the_enum_declares()
    {
        var clock = Clock.New();
        var task = TaskItems.RenewPassport(clock);
        task.ChangeStatus(TaskItemStatus.InProgress, clock);

        await fixture.SeedAsync(task);

        await using var command = fixture.DataSource.CreateCommand("SELECT status FROM tasks WHERE id = @id");
        command.Parameters.AddWithValue("id", task.Id.Value);

        // Read raw, without EF: 2 is InProgress, and TaskItemStatus's explicit numbering is a
        // persistence contract that a member reorder must break here.
        var status = await command.ExecuteScalarAsync();
        status.ShouldBe(2);
    }
}

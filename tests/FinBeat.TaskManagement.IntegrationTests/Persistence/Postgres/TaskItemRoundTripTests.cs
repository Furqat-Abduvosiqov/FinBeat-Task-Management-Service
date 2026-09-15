using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

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

        // Reference identity, not value equality: this fails on a null Description, and it fails just
        // as surely if Create ever stopped returning the shared None instance.
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

        // Read raw, without EF, so the number in the column is asserted rather than whatever the
        // converter would hand back. 2 is InProgress - the explicit numbering on TaskItemStatus is the
        // persistence contract, so a reordering of its members has to show up here.
        var status = await command.ExecuteScalarAsync();
        status.ShouldBe(2);
    }
}

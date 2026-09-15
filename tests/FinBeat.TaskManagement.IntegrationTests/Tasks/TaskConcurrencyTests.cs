using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Application.Tasks;
using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FinBeat.TaskManagement.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Tasks;

/// <summary>Proves a task that moved underneath a request is reported rather than overwritten.</summary>
/// <remarks>
/// The race is made deterministic: the context loads and tracks the row, another writer changes it
/// through raw SQL, and the handler then works from the copy it already holds - which is exactly the
/// state a second request would be in, without needing two requests to interleave by luck.
/// </remarks>
[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskConcurrencyTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Updating_a_task_someone_else_changed_reports_a_conflict()
    {
        var task = await SeedAsync();

        await using var context = fixture.CreateContext();
        await TrackAsync(context, task.Id);
        await ChangeBehindOurBackAsync(task.Id.Value);

        var result = await new UpdateTaskDetailsHandler(context, new RecordingIntegrationEventPublisher(), Clock.New())
            .HandleAsync(new UpdateTaskDetailsCommand(task.Id.Value, "Mine", null));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("task.concurrent-modification");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task Changing_the_status_of_a_task_someone_else_changed_reports_a_conflict()
    {
        var task = await SeedAsync();

        await using var context = fixture.CreateContext();
        await TrackAsync(context, task.Id);
        await ChangeBehindOurBackAsync(task.Id.Value);

        var result = await new ChangeTaskStatusHandler(context, new RecordingIntegrationEventPublisher(), Clock.New())
            .HandleAsync(new ChangeTaskStatusCommand(task.Id.Value, TaskItemStatus.InProgress));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task Deleting_a_task_someone_else_already_deleted_reports_not_found()
    {
        var task = await SeedAsync();

        await using var context = fixture.CreateContext();
        await TrackAsync(context, task.Id);

        await using var delete = fixture.DataSource.CreateCommand("DELETE FROM tasks WHERE id = @id");
        delete.Parameters.AddWithValue("id", task.Id.Value);
        await delete.ExecuteNonQueryAsync();

        var result = await new DeleteTaskHandler(context, new RecordingIntegrationEventPublisher(), Clock.New())
            .HandleAsync(new DeleteTaskCommand(task.Id.Value));

        // Nobody can change a task into existence, so a vanished row is gone rather than contested.
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    private async Task<TaskItem> SeedAsync()
    {
        var task = TaskItems.WithTitle($"Contested {Guid.NewGuid()}", Clock.New());
        await fixture.SeedAsync(task);

        return task;
    }

    private static async Task TrackAsync(DbContext context, TaskItemId id) =>
        await context.Set<TaskItem>().FirstAsync(task => task.Id == id);

    private async Task ChangeBehindOurBackAsync(Guid taskId)
    {
        await using var command = fixture.DataSource.CreateCommand(
            "UPDATE tasks SET title = 'Changed by someone else' WHERE id = @id");

        command.Parameters.AddWithValue("id", taskId);
        await command.ExecuteNonQueryAsync();
    }
}

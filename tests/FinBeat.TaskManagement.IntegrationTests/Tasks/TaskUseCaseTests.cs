using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Application.Tasks;
using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Application.Tasks.Queries;
using FinBeat.TaskManagement.Contracts.Tasks;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FinBeat.TaskManagement.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Tasks;

/// <summary>Drives every task use case against a real PostgreSQL, with the transport stood in for.</summary>
[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskUseCaseTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Creating_a_task_persists_it_and_publishes_TaskCreated()
    {
        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        var result = await new CreateTaskHandler(context, publisher, Clock.New())
            .HandleAsync(new CreateTaskCommand("  Renew passport  ", "  Before the trip in June  "));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe("Renew passport");
        result.Value.Description.ShouldBe("Before the trip in June");
        result.Value.Status.ShouldBe(nameof(TaskItemStatus.New));
        result.Value.CreatedAt.ShouldBe(result.Value.UpdatedAt);

        var stored = await ReadAsync(result.Value.Id);
        stored.Title.Value.ShouldBe("Renew passport");

        var published = publisher.Published.ShouldHaveSingleItem();
        published.ContractType.ShouldBe(typeof(TaskCreated));

        var created = published.Payload.ShouldBeOfType<TaskCreated>();
        created.TaskId.ShouldBe(result.Value.Id);
        created.Title.ShouldBe("Renew passport");
        created.Status.ShouldBe(nameof(TaskItemStatus.New));
    }

    [Fact]
    public async Task Nothing_is_persisted_when_publishing_fails()
    {
        // The publish happens before SaveChangesAsync so the event and the row commit together.
        // Were it the other way round, the task below would survive a transport that is down.
        const string Title = "Publish before save";

        await using var context = fixture.CreateContext();

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => new CreateTaskHandler(context, new FailingIntegrationEventPublisher(), Clock.New())
                .HandleAsync(new CreateTaskCommand(Title, null)));

        thrown.Message.ShouldBe(FailingIntegrationEventPublisher.FailureMessage);
        (await CountByTitleAsync(Title)).ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Creating_a_task_without_a_title_fails_validation_and_publishes_nothing(string? title)
    {
        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        var result = await new CreateTaskHandler(context, publisher, Clock.New())
            .HandleAsync(new CreateTaskCommand(title, null));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("task.title.invalid");
        result.Error.Type.ShouldBe(ErrorType.Validation);
        publisher.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reading_a_task_by_id_returns_what_was_created()
    {
        var created = await CreateAsync("Book the flights");

        await using var context = fixture.CreateContext();
        var result = await new GetTaskByIdHandler(context).HandleAsync(new GetTaskByIdQuery(created.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(created);
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Reading_an_unknown_task_reports_not_found(string taskId)
    {
        await using var context = fixture.CreateContext();

        var result = await new GetTaskByIdHandler(context).HandleAsync(new GetTaskByIdQuery(Guid.Parse(taskId)));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("task.not-found");
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Listing_by_status_leaves_out_every_other_status()
    {
        // The New task is created here rather than relied on from another test: run this one first
        // and every row in the table would be Archived, so dropping the filter would still pass.
        var excluded = await CreateAsync("Still new");
        var archived = await SeedArchivedAsync(new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero));

        await using var context = fixture.CreateContext();
        var result = await new GetTasksHandler(context).HandleAsync(new GetTasksQuery(TaskItemStatus.Archived));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldContain(task => task.Id == archived);
        result.Value.Items.ShouldNotContain(task => task.Id == excluded.Id);
        result.Value.Items.ShouldAllBe(task => task.Status == nameof(TaskItemStatus.Archived));
    }

    [Fact]
    public async Task Listing_returns_the_oldest_first()
    {
        // Older than anything else the suite seeds, so these two open page one however much has
        // accumulated. Seeded newest first, so the expected order reverses the insertion order.
        // Asked without a status, too: filtered by one, ix_tasks_status_created_at hands back rows
        // already sorted by created_at and dropping the OrderBy would go unnoticed.
        var later = await SeedArchivedAsync(new DateTimeOffset(2020, 1, 2, 9, 0, 0, TimeSpan.Zero));
        var earlier = await SeedArchivedAsync(new DateTimeOffset(2020, 1, 1, 9, 0, 0, TimeSpan.Zero));

        await using var context = fixture.CreateContext();
        var result = await new GetTasksHandler(context).HandleAsync(new GetTasksQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Take(2).Select(task => task.Id).ShouldBe([earlier, later]);
    }

    [Fact]
    public async Task Pages_neither_repeat_a_task_nor_skip_one()
    {
        // Older than everything else again, so these three are the first three rows in order.
        var seeded = new List<Guid>();

        for (var day = 1; day <= 3; day++)
        {
            seeded.Add(await SeedArchivedAsync(new DateTimeOffset(2019, 1, day, 9, 0, 0, TimeSpan.Zero)));
        }

        await using var context = fixture.CreateContext();
        var handler = new GetTasksHandler(context);

        var first = await handler.HandleAsync(new GetTasksQuery(Page: 1, PageSize: 2));
        var second = await handler.HandleAsync(new GetTasksQuery(Page: 2, PageSize: 2));

        first.Value.Items.Count.ShouldBe(2);
        first.Value.Items.Select(task => task.Id).ShouldBe(seeded.Take(2));
        second.Value.Items[0].Id.ShouldBe(seeded[2]);

        // The count is of everything matching, not of the page, so it cannot change between pages.
        first.Value.TotalItems.ShouldBe(second.Value.TotalItems);
        first.Value.TotalItems.ShouldBeGreaterThanOrEqualTo(3);
        first.Value.HasNext.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, GetTasksHandler.DefaultPageSize)]
    [InlineData(1, 0)]
    [InlineData(1, GetTasksHandler.MaxPageSize + 1)]
    public async Task A_page_outside_what_is_offered_is_rejected(int page, int pageSize)
    {
        await using var context = fixture.CreateContext();

        var result = await new GetTasksHandler(context).HandleAsync(new GetTasksQuery(Page: page, PageSize: pageSize));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("tasks.paging.invalid");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Updating_the_details_rewrites_the_row_and_publishes_TaskDetailsUpdated()
    {
        // One clock across both calls: a second Clock.New() would restart at the same instant, and
        // "UpdatedAt moved" could not fail.
        var clock = Clock.New();
        var created = await CreateAsync("Renew passport", clock);

        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        var result = await new UpdateTaskDetailsHandler(context, publisher, clock)
            .HandleAsync(new UpdateTaskDetailsCommand(created.Id, "Renew passport urgently", "The office closes in July"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe("Renew passport urgently");
        result.Value.CreatedAt.ShouldBe(created.CreatedAt);
        result.Value.UpdatedAt.ShouldBeGreaterThan(created.UpdatedAt);

        var stored = await ReadAsync(created.Id);
        stored.Title.Value.ShouldBe("Renew passport urgently");
        stored.Description.Value.ShouldBe("The office closes in July");

        var published = publisher.Published.ShouldHaveSingleItem();
        published.ContractType.ShouldBe(typeof(TaskDetailsUpdated));
        published.Payload.ShouldBeOfType<TaskDetailsUpdated>().Title.ShouldBe("Renew passport urgently");
    }

    [Fact]
    public async Task Changing_the_status_stores_it_and_publishes_both_ends_of_the_move()
    {
        var created = await CreateAsync("Pack the bags");

        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        var result = await new ChangeTaskStatusHandler(context, publisher, Clock.New())
            .HandleAsync(new ChangeTaskStatusCommand(created.Id, TaskItemStatus.InProgress));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(TaskItemStatus.InProgress));
        (await ReadAsync(created.Id)).Status.ShouldBe(TaskItemStatus.InProgress);

        var published = publisher.Published.ShouldHaveSingleItem();
        published.ContractType.ShouldBe(typeof(TaskStatusChanged));

        var changed = published.Payload.ShouldBeOfType<TaskStatusChanged>();
        changed.PreviousStatus.ShouldBe(nameof(TaskItemStatus.New));
        changed.CurrentStatus.ShouldBe(nameof(TaskItemStatus.InProgress));
    }

    [Fact]
    public async Task A_status_move_the_rules_forbid_reports_a_conflict()
    {
        var created = await CreateAsync("Archive me");

        await using (var archiveContext = fixture.CreateContext())
        {
            var archived = await new ChangeTaskStatusHandler(
                    archiveContext,
                    new RecordingIntegrationEventPublisher(),
                    Clock.New())
                .HandleAsync(new ChangeTaskStatusCommand(created.Id, TaskItemStatus.Archived));

            archived.IsSuccess.ShouldBeTrue();
        }

        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        // Archived restores to New and nowhere else, so this move is rejected by the domain.
        var result = await new ChangeTaskStatusHandler(context, publisher, Clock.New())
            .HandleAsync(new ChangeTaskStatusCommand(created.Id, TaskItemStatus.Completed));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("task.status.invalid-transition");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        publisher.Published.ShouldBeEmpty();
        (await ReadAsync(created.Id)).Status.ShouldBe(TaskItemStatus.Archived);
    }

    [Fact]
    public async Task A_status_outside_the_enum_is_rejected_by_the_handler_too()
    {
        // The API rejects this at the edge with FluentValidation. This is the backstop for every
        // other caller, and the reason the handler keeps its own check.
        var created = await CreateAsync("Undefined status at the handler");

        await using var context = fixture.CreateContext();

        var result = await new ChangeTaskStatusHandler(context, new RecordingIntegrationEventPublisher(), Clock.New())
            .HandleAsync(new ChangeTaskStatusCommand(created.Id, (TaskItemStatus)99));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("task.status.unknown");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Deleting_a_task_removes_the_row_and_publishes_TaskDeleted()
    {
        var created = await CreateAsync("Cancel the subscription");

        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        var result = await new DeleteTaskHandler(context, publisher, Clock.New())
            .HandleAsync(new DeleteTaskCommand(created.Id));

        result.IsSuccess.ShouldBeTrue();

        // The raw count, not an EF query: that is what tells a hard delete from a soft one.
        await using var command = fixture.DataSource.CreateCommand("SELECT count(*) FROM tasks WHERE id = @id");
        command.Parameters.AddWithValue("id", created.Id);
        ((long)(await command.ExecuteScalarAsync())!).ShouldBe(0);

        var published = publisher.Published.ShouldHaveSingleItem();
        published.ContractType.ShouldBe(typeof(TaskDeleted));
        published.Payload.ShouldBeOfType<TaskDeleted>().TaskId.ShouldBe(created.Id);
    }

    [Fact]
    public async Task Deleting_an_unknown_task_reports_not_found()
    {
        await using var context = fixture.CreateContext();
        var publisher = new RecordingIntegrationEventPublisher();

        var result = await new DeleteTaskHandler(context, publisher, Clock.New())
            .HandleAsync(new DeleteTaskCommand(Guid.NewGuid()));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        publisher.Published.ShouldBeEmpty();
    }

    private async Task<TaskResponse> CreateAsync(string title, TimeProvider? clock = null)
    {
        await using var context = fixture.CreateContext();

        var result = await new CreateTaskHandler(context, new RecordingIntegrationEventPublisher(), clock ?? Clock.New())
            .HandleAsync(new CreateTaskCommand(title, null));

        result.IsSuccess.ShouldBeTrue();

        return result.Value;
    }

    private async Task<Guid> SeedArchivedAsync(DateTimeOffset createdAt)
    {
        var clock = new FakeTimeProvider(createdAt) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };
        var task = TaskItems.WithTitle($"Archived at {createdAt:O}", clock);
        task.ChangeStatus(TaskItemStatus.Archived, clock);

        await fixture.SeedAsync(task);

        return task.Id.Value;
    }

    private async Task<TaskItem> ReadAsync(Guid taskId)
    {
        var id = new TaskItemId(taskId);

        await using var context = fixture.CreateContext();

        return await context.Tasks.SingleAsync(task => task.Id == id);
    }

    private async Task<long> CountByTitleAsync(string title)
    {
        await using var command = fixture.DataSource.CreateCommand("SELECT count(*) FROM tasks WHERE title = @title");
        command.Parameters.AddWithValue("title", title);

        return (long)(await command.ExecuteScalarAsync())!;
    }
}

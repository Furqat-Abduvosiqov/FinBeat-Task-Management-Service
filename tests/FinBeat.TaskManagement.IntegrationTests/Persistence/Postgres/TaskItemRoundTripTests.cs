using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskItemRoundTripTests
{
    private readonly PostgresFixture _fixture;

    public TaskItemRoundTripTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task A_saved_task_reloads_with_the_same_title_description_status_and_timestamps()
    {
        var clock = Clock.New();
        var task = TaskItem.Create(
            TaskTitle.Create("Renew passport"),
            TaskDescription.Create("Before the trip in June"),
            clock);
        task.ChangeStatus(TaskItemStatus.InProgress, clock);

        await using (var writeContext = _fixture.CreateContext())
        {
            writeContext.Tasks.Add(task);
            await writeContext.SaveChangesAsync();
        }

        // A fresh context per read, not ChangeTracker.Clear(): nothing here is served from the
        // identity map, and the read exercises a fresh pooled connection.
        await using var readContext = _fixture.CreateContext();
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
        var task = TaskItem.Create(TaskTitle.Create("Renew passport"), TaskDescription.None, clock);

        await using (var writeContext = _fixture.CreateContext())
        {
            writeContext.Tasks.Add(task);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.Tasks.SingleAsync(t => t.Id == task.Id);

        // Reference identity, not value equality: this fails on a null Description, and it fails just
        // as surely if Create ever stopped returning the shared None instance.
        reloaded.Description.ShouldBeSameAs(TaskDescription.None);
    }

    [Fact]
    public async Task Status_is_stored_as_the_native_enum_label_not_an_integer()
    {
        var clock = Clock.New();
        var task = TaskItem.Create(TaskTitle.Create("Renew passport"), TaskDescription.None, clock);
        task.ChangeStatus(TaskItemStatus.InProgress, clock);

        await using (var writeContext = _fixture.CreateContext())
        {
            writeContext.Tasks.Add(task);
            await writeContext.SaveChangesAsync();
        }

        await using var command = _fixture.DataSource.CreateCommand("SELECT status::text FROM tasks WHERE id = @id");
        command.Parameters.AddWithValue("id", task.Id.Value);

        // The moment someone adds .HasConversion<int>(), this reads back "2" instead of "in_progress".
        var status = await command.ExecuteScalarAsync();
        status.ShouldBe("in_progress");
    }

    [Fact]
    public async Task The_postgres_enum_type_carries_the_expected_labels_in_declaration_order()
    {
        await using var command = _fixture.DataSource.CreateCommand("""
            SELECT e.enumlabel
            FROM pg_enum e
            JOIN pg_type t ON t.oid = e.enumtypid
            WHERE t.typname = @typeName
            ORDER BY e.enumsortorder
            """);
        command.Parameters.AddWithValue("typeName", PostgresEnumMapping.TaskItemStatusTypeName);

        var labels = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            labels.Add(reader.GetString(0));
        }

        labels.ShouldBe(["new", "in_progress", "completed", "archived"]);
    }

    [Fact]
    public async Task Counting_by_status_proves_the_parameter_is_written_as_the_enum_type()
    {
        var clock = Clock.New();
        var matching = TaskItem.Create(TaskTitle.Create("Renew passport"), TaskDescription.None, clock);
        matching.ChangeStatus(TaskItemStatus.InProgress, clock);
        var nonMatching = TaskItem.Create(TaskTitle.Create("Buy milk"), TaskDescription.None, clock);

        await using (var writeContext = _fixture.CreateContext())
        {
            writeContext.Tasks.AddRange(matching, nonMatching);
            await writeContext.SaveChangesAsync();
        }

        // If the ADO-side MapEnum were missing, this would throw before it ever got to counting
        // anything. Scoped to these two ids so other tests' rows in the shared database cannot change
        // the expected count.
        var ids = new[] { matching.Id, nonMatching.Id };
        await using var readContext = _fixture.CreateContext();
        var count = await readContext.Tasks.CountAsync(t => t.Status == TaskItemStatus.InProgress && ids.Contains(t.Id));

        count.ShouldBe(1);
    }
}

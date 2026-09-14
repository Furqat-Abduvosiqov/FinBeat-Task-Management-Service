using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.Events;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskItemTests
{
    private static TaskTitle Title => TaskTitle.Create("Buy milk");

    private static TaskDescription Description => TaskDescription.Create("2%, from the corner store");

    [Fact]
    public void Create_sets_title_description_status_and_matching_timestamps()
    {
        // AutoAdvanceAmount makes every read of the clock return a different instant. Without it a
        // FakeTimeProvider pinned at a fixed time returns the same value from every read, so
        // rewriting Create as two separate GetUtcNow() calls — the exact regression the
        // "one single clock read" requirement exists to prevent — would leave this test green.
        var start = DateTimeOffset.UnixEpoch;
        var clock = new FakeTimeProvider(start) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };

        var task = TaskItem.Create(Title, Description, clock);

        task.Title.ShouldBe(Title);
        task.Description.ShouldBe(Description);
        task.Status.ShouldBe(TaskItemStatus.New);
        task.CreatedAt.ShouldBe(start);
        task.UpdatedAt.ShouldBe(start);
        task.CreatedAt.ShouldBe(task.UpdatedAt);
    }

    [Fact]
    public void Create_assigns_a_non_empty_id()
    {
        var (task, _) = NewTask();

        task.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_raises_exactly_one_created_event_with_matching_payload()
    {
        var (task, clock) = NewTask();

        var domainEvent = SingleEvent<TaskItemCreatedDomainEvent>(task);
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.Title.ShouldBe(task.Title);
        domainEvent.Description.ShouldBe(task.Description);
        domainEvent.Status.ShouldBe(TaskItemStatus.New);
        domainEvent.OccurredOnUtc.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public void Create_throws_when_any_argument_is_null()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        Should.Throw<ArgumentNullException>(() => TaskItem.Create(null!, Description, clock));
        Should.Throw<ArgumentNullException>(() => TaskItem.Create(Title, null!, clock));
        Should.Throw<ArgumentNullException>(() => TaskItem.Create(Title, Description, null!));
    }

    [Fact]
    public void UpdateDetails_with_changed_values_assigns_them_bumps_UpdatedAt_and_leaves_CreatedAt_untouched()
    {
        var (task, clock) = NewTask();
        task.ClearDomainEvents();
        var createdAt = task.CreatedAt;

        clock.Advance(TimeSpan.FromHours(1));
        var newTitle = TaskTitle.Create("Buy oat milk");
        var newDescription = TaskDescription.Create("Unsweetened");

        task.UpdateDetails(newTitle, newDescription, clock);

        task.Title.ShouldBe(newTitle);
        task.Description.ShouldBe(newDescription);
        task.UpdatedAt.ShouldBe(clock.GetUtcNow());
        task.CreatedAt.ShouldBe(createdAt);

        var domainEvent = SingleEvent<TaskItemDetailsUpdatedDomainEvent>(task);
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.Title.ShouldBe(newTitle);
        domainEvent.Description.ShouldBe(newDescription);
        domainEvent.OccurredOnUtc.ShouldBe(task.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_with_values_equal_to_current_raises_no_event_and_does_not_bump_UpdatedAt()
    {
        var (task, clock) = NewTask();
        task.ClearDomainEvents();
        var updatedAt = task.UpdatedAt;

        clock.Advance(TimeSpan.FromHours(1));
        task.UpdateDetails(TaskTitle.Create(Title.Value), TaskDescription.Create(Description.Value), clock);

        task.UpdatedAt.ShouldBe(updatedAt);
        task.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateDetails_throws_when_any_argument_is_null()
    {
        var (task, clock) = NewTask();

        Should.Throw<ArgumentNullException>(() => task.UpdateDetails(null!, Description, clock));
        Should.Throw<ArgumentNullException>(() => task.UpdateDetails(Title, null!, clock));
        Should.Throw<ArgumentNullException>(() => task.UpdateDetails(Title, Description, null!));
    }

    [Fact]
    public void ChangeStatus_to_a_legal_target_assigns_it_bumps_UpdatedAt_and_raises_event_with_both_statuses()
    {
        var (task, clock) = NewTask();
        task.ClearDomainEvents();

        clock.Advance(TimeSpan.FromMinutes(30));
        task.ChangeStatus(TaskItemStatus.InProgress, clock);

        task.Status.ShouldBe(TaskItemStatus.InProgress);
        task.UpdatedAt.ShouldBe(clock.GetUtcNow());

        var domainEvent = SingleEvent<TaskItemStatusChangedDomainEvent>(task);
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.PreviousStatus.ShouldBe(TaskItemStatus.New);
        domainEvent.CurrentStatus.ShouldBe(TaskItemStatus.InProgress);
        domainEvent.OccurredOnUtc.ShouldBe(task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_to_the_current_status_raises_no_event_and_does_not_bump_UpdatedAt()
    {
        var (task, clock) = NewTask();
        task.ClearDomainEvents();
        var updatedAt = task.UpdatedAt;

        clock.Advance(TimeSpan.FromMinutes(30));
        task.ChangeStatus(TaskItemStatus.New, clock);

        task.Status.ShouldBe(TaskItemStatus.New);
        task.UpdatedAt.ShouldBe(updatedAt);
        task.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeStatus_along_a_rejected_edge_throws_and_leaves_status_and_UpdatedAt_untouched()
    {
        var (task, clock) = NewTask();
        task.ChangeStatus(TaskItemStatus.Completed, clock);
        task.ClearDomainEvents();
        var updatedAt = task.UpdatedAt;

        clock.Advance(TimeSpan.FromMinutes(10));

        var exception = Should.Throw<InvalidTaskStatusTransitionException>(() => task.ChangeStatus(TaskItemStatus.New, clock));

        exception.From.ShouldBe(TaskItemStatus.Completed);
        exception.To.ShouldBe(TaskItemStatus.New);
        task.Status.ShouldBe(TaskItemStatus.Completed);
        task.UpdatedAt.ShouldBe(updatedAt);
        task.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeStatus_with_a_null_clock_throws()
    {
        var (task, _) = NewTask();

        Should.Throw<ArgumentNullException>(() => task.ChangeStatus(TaskItemStatus.InProgress, null!));
    }

    [Fact]
    public void Archive_then_restore_round_trips_new_to_archived_to_new()
    {
        var (task, clock) = NewTask();

        task.ChangeStatus(TaskItemStatus.Archived, clock);
        task.Status.ShouldBe(TaskItemStatus.Archived);

        task.ChangeStatus(TaskItemStatus.New, clock);
        task.Status.ShouldBe(TaskItemStatus.New);
    }

    [Fact]
    public void Delete_raises_exactly_one_deleted_event()
    {
        var (task, clock) = NewTask();
        task.ClearDomainEvents();

        clock.Advance(TimeSpan.FromMinutes(1));
        task.Delete(clock);

        var domainEvent = SingleEvent<TaskItemDeletedDomainEvent>(task);
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.OccurredOnUtc.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public void Delete_works_on_an_archived_task()
    {
        var (task, clock) = NewTask();
        task.ChangeStatus(TaskItemStatus.Archived, clock);
        task.ClearDomainEvents();

        Should.NotThrow(() => task.Delete(clock));

        SingleEvent<TaskItemDeletedDomainEvent>(task);
    }

    [Fact]
    public void Delete_leaves_status_and_updated_at_untouched()
    {
        var (task, clock) = NewTask();
        task.ChangeStatus(TaskItemStatus.InProgress, clock);
        var statusBefore = task.Status;
        var updatedAtBefore = task.UpdatedAt;
        task.ClearDomainEvents();

        clock.Advance(TimeSpan.FromMinutes(5));
        task.Delete(clock);

        // Delete records intent only — the row is removed by whoever commits, so there is nothing on
        // the aggregate for it to mutate. Pins that against a future edit adding an UpdatedAt bump.
        task.Status.ShouldBe(statusBefore);
        task.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    [Fact]
    public void Delete_with_a_null_clock_throws()
    {
        var (task, _) = NewTask();

        Should.Throw<ArgumentNullException>(() => task.Delete(null!));
    }

    [Fact]
    public void ClearDomainEvents_between_operations_lets_each_operations_events_be_asserted_in_isolation()
    {
        var (task, clock) = NewTask();
        task.DomainEvents.Count.ShouldBe(1);

        task.ClearDomainEvents();
        task.ChangeStatus(TaskItemStatus.InProgress, clock);
        SingleEvent<TaskItemStatusChangedDomainEvent>(task);

        task.ClearDomainEvents();
        task.Delete(clock);
        SingleEvent<TaskItemDeletedDomainEvent>(task);
    }

    // Every test starts from the epoch on a clock it can advance. Keeping that here means changing
    // the convention is one edit rather than sixteen.
    private static (TaskItem Task, FakeTimeProvider Clock) NewTask()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        return (TaskItem.Create(Title, Description, clock), clock);
    }

    private static TEvent SingleEvent<TEvent>(TaskItem task)
        where TEvent : IDomainEvent =>
        task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TEvent>();
}

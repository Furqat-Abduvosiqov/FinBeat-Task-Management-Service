using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.Events;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;
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
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var task = TaskItem.Create(Title, Description, clock);

        task.Title.ShouldBe(Title);
        task.Description.ShouldBe(Description);
        task.Status.ShouldBe(TaskItemStatus.New);
        task.CreatedAt.ShouldBe(clock.GetUtcNow());
        task.UpdatedAt.ShouldBe(clock.GetUtcNow());
        task.CreatedAt.ShouldBe(task.UpdatedAt);
    }

    [Fact]
    public void Create_assigns_a_non_empty_id()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var task = TaskItem.Create(Title, Description, clock);

        task.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_raises_exactly_one_created_event_with_matching_payload()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var task = TaskItem.Create(Title, Description, clock);

        var domainEvent = task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemCreatedDomainEvent>();
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
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
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

        var domainEvent = task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemDetailsUpdatedDomainEvent>();
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.Title.ShouldBe(newTitle);
        domainEvent.Description.ShouldBe(newDescription);
        domainEvent.OccurredOnUtc.ShouldBe(task.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_with_values_equal_to_current_raises_no_event_and_does_not_bump_UpdatedAt()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
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
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);

        Should.Throw<ArgumentNullException>(() => task.UpdateDetails(null!, Description, clock));
        Should.Throw<ArgumentNullException>(() => task.UpdateDetails(Title, null!, clock));
        Should.Throw<ArgumentNullException>(() => task.UpdateDetails(Title, Description, null!));
    }

    [Fact]
    public void ChangeStatus_to_a_legal_target_assigns_it_bumps_UpdatedAt_and_raises_event_with_both_statuses()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
        task.ClearDomainEvents();

        clock.Advance(TimeSpan.FromMinutes(30));
        task.ChangeStatus(TaskItemStatus.InProgress, clock);

        task.Status.ShouldBe(TaskItemStatus.InProgress);
        task.UpdatedAt.ShouldBe(clock.GetUtcNow());

        var domainEvent = task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemStatusChangedDomainEvent>();
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.PreviousStatus.ShouldBe(TaskItemStatus.New);
        domainEvent.CurrentStatus.ShouldBe(TaskItemStatus.InProgress);
        domainEvent.OccurredOnUtc.ShouldBe(task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_to_the_current_status_raises_no_event_and_does_not_bump_UpdatedAt()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
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
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
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
    public void Archive_then_restore_round_trips_new_to_archived_to_new()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);

        task.ChangeStatus(TaskItemStatus.Archived, clock);
        task.Status.ShouldBe(TaskItemStatus.Archived);

        task.ChangeStatus(TaskItemStatus.New, clock);
        task.Status.ShouldBe(TaskItemStatus.New);
    }

    [Fact]
    public void Delete_raises_exactly_one_deleted_event()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
        task.ClearDomainEvents();

        clock.Advance(TimeSpan.FromMinutes(1));
        task.Delete(clock);

        var domainEvent = task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemDeletedDomainEvent>();
        domainEvent.TaskId.ShouldBe(task.Id);
        domainEvent.OccurredOnUtc.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public void Delete_works_on_an_archived_task()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
        task.ChangeStatus(TaskItemStatus.Archived, clock);
        task.ClearDomainEvents();

        Should.NotThrow(() => task.Delete(clock));

        task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemDeletedDomainEvent>();
    }

    [Fact]
    public void ClearDomainEvents_between_operations_lets_each_operations_events_be_asserted_in_isolation()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var task = TaskItem.Create(Title, Description, clock);
        task.DomainEvents.Count.ShouldBe(1);

        task.ClearDomainEvents();
        task.ChangeStatus(TaskItemStatus.InProgress, clock);
        task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemStatusChangedDomainEvent>();

        task.ClearDomainEvents();
        task.Delete(clock);
        task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskItemDeletedDomainEvent>();
    }
}

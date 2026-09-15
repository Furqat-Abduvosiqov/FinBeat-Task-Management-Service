using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Contracts.Tasks;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.Events;

namespace FinBeat.TaskManagement.Application.Tasks;

internal static class TaskIntegrationEventPublishing
{
    /// <summary>Publishes everything the aggregate has raised, then clears it.</summary>
    /// <remarks>Call before SaveChangesAsync, so the event and the change that caused it commit together.</remarks>
    internal static async Task PublishRaisedEventsAsync(
        this IIntegrationEventPublisher publisher,
        TaskItem task,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in task.DomainEvents)
        {
            await PublishAsync(publisher, domainEvent, cancellationToken);
        }

        task.ClearDomainEvents();
    }

    // Each branch publishes its concrete contract type, never IDomainEvent or object: the transport
    // routes on the static type, so a widened one would be delivered to nobody.
    private static Task PublishAsync(
        IIntegrationEventPublisher publisher,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken) => domainEvent switch
    {
        TaskItemCreatedDomainEvent created => publisher.PublishAsync(
            new TaskCreated(
                created.TaskId.Value,
                created.Title.Value,
                created.Description.Value,
                created.Status.ToString(),
                created.OccurredOnUtc),
            cancellationToken),

        TaskItemDetailsUpdatedDomainEvent updated => publisher.PublishAsync(
            new TaskDetailsUpdated(
                updated.TaskId.Value,
                updated.Title.Value,
                updated.Description.Value,
                updated.OccurredOnUtc),
            cancellationToken),

        TaskItemStatusChangedDomainEvent changed => publisher.PublishAsync(
            new TaskStatusChanged(
                changed.TaskId.Value,
                changed.PreviousStatus.ToString(),
                changed.CurrentStatus.ToString(),
                changed.OccurredOnUtc),
            cancellationToken),

        TaskItemDeletedDomainEvent deleted => publisher.PublishAsync(
            new TaskDeleted(deleted.TaskId.Value, deleted.OccurredOnUtc),
            cancellationToken),

        _ => throw new NotSupportedException(
            $"No integration contract is mapped for '{domainEvent.GetType().Name}'."),
    };
}

namespace FinBeat.TaskManagement.Application.Abstractions;

/// <summary>Publishes integration events onto whatever transport Infrastructure has configured.</summary>
/// <remarks>A port, not MassTransit directly: the transport stays Infrastructure's decision.</remarks>
public interface IIntegrationEventPublisher
{
    /// <summary>Publishes one integration event.</summary>
    /// <typeparam name="TEvent">The contract type, from <c>FinBeat.TaskManagement.Contracts</c>.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes once the event is recorded for delivery; with the outbox it is not sent until the transaction commits.</returns>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}

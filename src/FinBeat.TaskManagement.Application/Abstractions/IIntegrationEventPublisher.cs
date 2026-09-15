namespace FinBeat.TaskManagement.Application.Abstractions;

/// <summary>Publishes integration events onto whatever transport Infrastructure has configured.</summary>
/// <remarks>
/// A port rather than MassTransit's own publish endpoint, because the architecture rules allow this
/// layer EF Core and nothing else. A use case says what happened; which broker hears about it, and
/// whether the publish is transactional, stays Infrastructure's decision.
/// </remarks>
public interface IIntegrationEventPublisher
{
    /// <summary>Publishes one integration event.</summary>
    /// <typeparam name="TEvent">The contract type, from <c>FinBeat.TaskManagement.Contracts</c>.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes once the event has been handed to the transport.</returns>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}

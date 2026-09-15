using FinBeat.TaskManagement.Application.Abstractions;
using MassTransit;

namespace FinBeat.TaskManagement.Infrastructure.Messaging;

/// <summary>Publishes integration events through MassTransit.</summary>
/// <remarks>
/// Scoped alongside the DbContext on purpose. With the bus outbox enabled, a publish through this
/// endpoint is written to the outbox table inside the same transaction as the change that caused it,
/// and delivered afterwards - so a task is never saved without its event, nor an event sent for a
/// task that was rolled back.
/// </remarks>
internal sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}

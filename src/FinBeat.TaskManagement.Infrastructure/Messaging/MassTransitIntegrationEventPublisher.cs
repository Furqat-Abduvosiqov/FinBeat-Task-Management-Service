using FinBeat.TaskManagement.Application.Abstractions;
using MassTransit;

namespace FinBeat.TaskManagement.Infrastructure.Messaging;

/// <summary>Publishes integration events through MassTransit.</summary>
/// <remarks>Scoped alongside the DbContext, so a publish lands in the outbox inside the same transaction.</remarks>
internal sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}

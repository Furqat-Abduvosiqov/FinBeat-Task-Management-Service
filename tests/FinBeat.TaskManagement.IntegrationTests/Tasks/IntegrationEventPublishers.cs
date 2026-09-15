using FinBeat.TaskManagement.Application.Abstractions;

namespace FinBeat.TaskManagement.IntegrationTests.Tasks;

/// <summary>An <see cref="IIntegrationEventPublisher"/> that keeps what it was asked to publish.</summary>
internal sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly List<PublishedEvent> _published = [];

    /// <summary>What has been published so far, in order.</summary>
    internal IReadOnlyList<PublishedEvent> Published => _published;

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        _published.Add(new PublishedEvent(typeof(TEvent), integrationEvent));

        return Task.CompletedTask;
    }

    /// <summary>One publish call.</summary>
    /// <param name="ContractType">The type argument it was published under.</param>
    /// <param name="Payload">The event itself.</param>
    /// <remarks>The declared type is recorded as well as the payload because a transport routes on the static type: published through a base type an event reaches nobody, and asserting on the payload's runtime type would not notice.</remarks>
    internal sealed record PublishedEvent(Type ContractType, object Payload);
}

/// <summary>An <see cref="IIntegrationEventPublisher"/> that always fails, standing in for a transport that is down.</summary>
internal sealed class FailingIntegrationEventPublisher : IIntegrationEventPublisher
{
    /// <summary>The message the failure carries.</summary>
    internal const string FailureMessage = "The transport is unavailable.";

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class =>
        throw new InvalidOperationException(FailureMessage);
}

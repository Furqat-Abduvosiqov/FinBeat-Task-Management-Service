namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>
/// Marks a type as a domain event: a statement that something has already happened inside the
/// domain, raised by an <see cref="AggregateRoot{TId}"/> as a side effect of its own behaviour.
/// </summary>
/// <remarks>
/// Deliberately carries no identifier of its own. An idempotency key for at-least-once redelivery
/// is a transport concern that belongs to the outbox/message-broker machinery in the
/// Infrastructure layer — the domain model has no notion of "redelivery" and should not be made to
/// carry a field that only makes sense once an event leaves the process.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>The instant, in UTC, at which the event occurred.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}

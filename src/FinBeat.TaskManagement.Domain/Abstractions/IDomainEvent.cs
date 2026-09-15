namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>Something that has already happened in the domain, raised by the aggregate it happened to.</summary>
/// <remarks>Never serialized: Application maps each one to a flat integration contract first. No event id - idempotency belongs to the outbox.</remarks>
public interface IDomainEvent
{
    /// <summary>When the event occurred, in UTC.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}

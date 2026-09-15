namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>Something that has already happened in the domain, raised by the aggregate it happened to.</summary>
/// <remarks>
/// <para>
/// Domain events never leave the process in this shape. Application maps each one to a flat
/// integration contract of primitives, and only that gets serialized. Value objects here have
/// private constructors, so <c>System.Text.Json</c> can write them but not read them back - a
/// failure that would otherwise surface only when a publisher tries to rehydrate an outbox row.
/// </para>
/// <para>
/// No event id either: an idempotency key only means something once an event leaves the process,
/// so it belongs to the outbox in Infrastructure.
/// </para>
/// </remarks>
public interface IDomainEvent
{
    /// <summary>When the event occurred, in UTC.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}

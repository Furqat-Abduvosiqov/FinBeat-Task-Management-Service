namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots: entities that are also the transactional consistency boundary
/// and the sole source of the domain events raised while enforcing it.
/// </summary>
/// <remarks>
/// <see cref="Raise"/> is <see langword="protected"/> so only the aggregate itself can add an
/// event, and <see cref="DomainEvents"/> exposes a read-only view over the internal list so nothing
/// outside the aggregate can inject or remove one directly. Together these guarantee that every
/// state change performed through the aggregate's public API has a corresponding event recorded
/// alongside it — there is no code path that changes state without the aggregate also saying so.
/// </remarks>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Initializes a new aggregate root with the given identity.</summary>
    /// <param name="id">The aggregate's identifier, assigned once and never changed afterwards.</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Parameterless constructor reserved for EF Core materialization.</summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// The domain events raised by this aggregate since construction or the last call to
    /// <see cref="ClearDomainEvents"/>. The returned collection is a read-only wrapper over the
    /// aggregate's internal list: it can be enumerated, but not added to or cleared, even by a
    /// caller that downcasts it to a mutable collection interface.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records that <paramref name="domainEvent"/> occurred as a result of this aggregate's own behaviour.</summary>
    /// <param name="domainEvent">The event to record.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Empties the recorded domain events. Called by the persistence layer once it has dispatched
    /// them, so that the same event is never published twice.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

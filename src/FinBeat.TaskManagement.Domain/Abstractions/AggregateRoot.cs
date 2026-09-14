namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>Base class for aggregate roots: the consistency boundary, and the only thing that raises domain events.</summary>
/// <remarks>
/// <see cref="Raise"/> is protected and <see cref="DomainEvents"/> is read-only, so an aggregate is
/// the only thing that can record its own events. That is what makes "state changed" and "an event
/// exists" inseparable.
/// </remarks>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Creates an aggregate root with the given identity.</summary>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Reserved for EF Core, which sets the properties by reflection afterwards.</summary>
    protected AggregateRoot()
    {
    }

    /// <summary>Events raised since construction or the last <see cref="ClearDomainEvents"/>. Cannot be mutated by callers.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records an event raised by this aggregate's own behaviour.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Empties the recorded events. Called after dispatch, so none is published twice.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

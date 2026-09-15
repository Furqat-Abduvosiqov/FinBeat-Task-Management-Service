namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>Base class for aggregate roots: the consistency boundary, and the only thing that raises domain events.</summary>
/// <remarks>Only an aggregate can record its own events, which keeps the state change and the event together.</remarks>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Events raised since construction or the last <see cref="ClearDomainEvents"/>. Cannot be mutated by callers.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records an event raised by this aggregate's own behaviour.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Empties the recorded events. Called after dispatch, so none is published twice.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

using FinBeat.TaskManagement.Domain.Abstractions;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Abstractions;

public sealed class AggregateRootTests
{
    [Fact]
    public void Raise_makes_the_event_visible_via_DomainEvents()
    {
        var aggregate = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent(DateTimeOffset.UnixEpoch);

        aggregate.RaiseTestEvent(domainEvent);

        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void ClearDomainEvents_empties_the_collection()
    {
        var aggregate = new TestAggregateRoot();
        aggregate.RaiseTestEvent(new TestDomainEvent(DateTimeOffset.UnixEpoch));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_cannot_be_mutated_by_a_caller_casting_it()
    {
        var aggregate = new TestAggregateRoot();
        aggregate.RaiseTestEvent(new TestDomainEvent(DateTimeOffset.UnixEpoch));

        var events = aggregate.DomainEvents;

        // AsReadOnly() wraps the internal list in a ReadOnlyCollection<T>, never the list itself,
        // so a cast back to the concrete mutable type must fail.
        events.ShouldNotBeOfType<List<IDomainEvent>>();

        // ReadOnlyCollection<T> does implement ICollection<T> for enumeration purposes, but every
        // mutating member throws rather than reaching through to the wrapped list.
        var mutable = events.ShouldBeAssignableTo<ICollection<IDomainEvent>>();
        Should.Throw<NotSupportedException>(() => mutable!.Add(new TestDomainEvent(DateTimeOffset.UnixEpoch)));
        aggregate.DomainEvents.Count.ShouldBe(1);
    }

    private sealed class TestAggregateRoot : AggregateRoot
    {
        public void RaiseTestEvent(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOnUtc) : IDomainEvent;
}

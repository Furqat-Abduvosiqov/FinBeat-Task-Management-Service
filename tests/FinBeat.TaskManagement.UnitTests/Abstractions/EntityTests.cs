using FinBeat.TaskManagement.Domain.Abstractions;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Abstractions;

public class EntityTests
{
    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Entities_of_the_same_type_with_different_ids_are_not_equal()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Equals(second).ShouldBeFalse();
        (first == second).ShouldBeFalse();
        (first != second).ShouldBeTrue();
    }

    [Fact]
    public void Entities_of_different_runtime_types_with_the_same_id_are_not_equal()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);
        var otherEntity = new OtherTestEntity(id);

        entity.Equals(otherEntity).ShouldBeFalse();
    }

    [Fact]
    public void An_entity_is_not_equal_to_null()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
        (entity != null).ShouldBeTrue();
    }

    [Fact]
    public void Raise_makes_the_event_visible_via_DomainEvents()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(DateTimeOffset.UnixEpoch);

        aggregate.RaiseTestEvent(domainEvent);

        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void ClearDomainEvents_empties_the_collection()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.RaiseTestEvent(new TestDomainEvent(DateTimeOffset.UnixEpoch));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_cannot_be_mutated_by_a_caller_casting_it()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
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

    [Fact]
    public void Entities_without_an_assigned_id_are_not_equal_to_each_other()
    {
        // The state EF Core leaves an instance in between calling the parameterless constructor and
        // populating it. Both carry Guid.Empty, so a naive Id.Equals(other.Id) would report them as
        // the same entity — and a HashSet would then silently keep only one of them.
        var first = new UnidentifiedTestEntity();
        var second = new UnidentifiedTestEntity();

        first.Equals(second).ShouldBeFalse();
        (first == second).ShouldBeFalse();
        new HashSet<Entity<Guid>> { first, second }.Count.ShouldBe(2);
    }

    [Fact]
    public void An_entity_without_an_assigned_id_is_still_equal_to_itself()
    {
        var entity = new UnidentifiedTestEntity();
        var sameReference = entity;

        entity.Equals(sameReference).ShouldBeTrue();
        (entity == sameReference).ShouldBeTrue();
    }

    [Fact]
    public void An_entity_with_a_reference_type_id_does_not_throw_before_the_id_is_assigned()
    {
        // TId is constrained to notnull, but that permits reference types, whose Id is null after
        // the parameterless constructor. Id.Equals(other.Id) would throw NullReferenceException.
        var first = new UnidentifiedStringKeyedEntity();
        var second = new UnidentifiedStringKeyedEntity();

        Should.NotThrow(() => first.Equals(second)).ShouldBeFalse();
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id)
    {
    }

    private sealed class OtherTestEntity(Guid id) : Entity<Guid>(id)
    {
    }

    /// <summary>Stands in for an entity EF Core has constructed but not yet populated.</summary>
    private sealed class UnidentifiedTestEntity : Entity<Guid>
    {
    }

    private sealed class UnidentifiedStringKeyedEntity : Entity<string>
    {
    }

    private sealed class TestAggregateRoot(Guid id) : AggregateRoot<Guid>(id)
    {
        public void RaiseTestEvent(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOnUtc) : IDomainEvent;
}

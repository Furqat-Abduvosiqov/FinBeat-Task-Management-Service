using System.Reflection;
using Shouldly;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// The shape of the model itself, not the layering around it. Whether an aggregate still enforces
// its own invariants is not something a dependency graph can see.
public sealed class DomainModelTests
{
    private const string AggregateRootBaseType = "FinBeat.TaskManagement.Domain.Abstractions.AggregateRoot`1";

    private const string DomainEventInterface = "FinBeat.TaskManagement.Domain.Abstractions.IDomainEvent";

    private static readonly Assembly Domain = ArchitectureModel.LoadAssembly(ArchitectureModel.Domain);

    // State changes go through methods because the method is what raises the event. One public
    // setter and state can change with no event, which breaks the listener and fails nothing.
    [Fact]
    public void Aggregate_roots_expose_no_public_setters()
    {
        var aggregateRoots = Domain.GetTypes().Where(IsAggregateRoot).ToArray();

        aggregateRoots.ShouldNotBeEmpty(
            $"this rule is vacuous if it finds no aggregate roots - check that '{AggregateRootBaseType}' still "
            + "names the base type, because a rename would make this test pass by finding nothing");

        var offenders = aggregateRoots
            .SelectMany(aggregateRoot => aggregateRoot
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{aggregateRoot.Name}.{property.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            "an aggregate's state may only change through a method that also raises the corresponding "
            + "domain event. Make the setter private and add a method that expresses the intent");
    }

    // Sealed because these are messages, not a type hierarchy: a subclass of an event would be
    // dispatched as its base type and its extra payload silently dropped.
    [Fact]
    public void Domain_event_types_are_sealed()
    {
        var events = DomainEventTypes();

        events.ShouldNotBeEmpty(
            $"this rule is vacuous if it finds no events - check that '{DomainEventInterface}' "
            + "still names the interface");

        var offenders = events
            .Where(type => !type.IsSealed)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty("domain events are messages and must not be inherited from");
    }

    // Catches the reverse mistake from the rule above: a type named like an event that never
    // implements the interface is never dispatched, and nothing else would notice.
    [Fact]
    public void Types_named_as_domain_events_implement_the_domain_event_interface()
    {
        var named = Domain.GetTypes()
            // Interfaces excluded: IDomainEvent itself matches the name and cannot implement itself.
            .Where(type => type is { IsPublic: true, IsInterface: false })
            .Where(type => type.Name.EndsWith("DomainEvent", StringComparison.Ordinal))
            .ToArray();

        named.ShouldNotBeEmpty("this rule is vacuous if it finds no types named '*DomainEvent'");

        var offenders = named
            .Where(type => !ImplementsDomainEvent(type))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            $"a type named as a domain event that does not implement {DomainEventInterface} is "
            + "never dispatched");
    }

    // Value types included: filtering to classes would skip an event declared as a record struct,
    // and the guards above would still pass on the strength of the existing ones.
    //
    // Abstract types included too, which is the point of excluding only interfaces here. Filtering on
    // IsAbstract would let an abstract BaseDomainEvent : IDomainEvent through untouched, and its
    // sealed subclasses would satisfy every other rule - producing exactly the event hierarchy the
    // sealing rule exists to prevent. An abstract event is not sealed, so it now fails that rule.
    private static Type[] DomainEventTypes() =>
        Domain.GetTypes()
            .Where(type => type is { IsPublic: true, IsInterface: false })
            .Where(ImplementsDomainEvent)
            .ToArray();

    private static bool ImplementsDomainEvent(Type type) =>
        Array.Exists(
            type.GetInterfaces(),
            contract => string.Equals(contract.FullName, DomainEventInterface, StringComparison.Ordinal));

    private static bool IsAggregateRoot(Type type)
    {
        if (type.IsAbstract || !type.IsClass)
        {
            return false;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType
                && string.Equals(
                    current.GetGenericTypeDefinition().FullName,
                    AggregateRootBaseType,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

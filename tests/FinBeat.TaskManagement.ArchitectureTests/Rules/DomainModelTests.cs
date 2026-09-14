using System.Reflection;
using Shouldly;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// Rules about the shape of the domain model itself, rather than the layering around it. The other
// rules here ask which layer may reference which; these ask whether the model still enforces its own
// invariants, which is a property no dependency graph can see.
public sealed class DomainModelTests
{
    private const string EntityBaseType = "FinBeat.TaskManagement.Domain.Abstractions.Entity`1";

    private const string DomainEventInterface = "FinBeat.TaskManagement.Domain.Abstractions.IDomainEvent";

    private static readonly Assembly Domain = ArchitectureModel.LoadAssembly(ArchitectureModel.Domain);

    // Every state change on an aggregate has to go through a method, because the method is what
    // raises the domain event. One public setter and a caller can change state without an event
    // existing, which silently breaks the listener the specification requires - and nothing fails.
    [Fact]
    public void Entities_expose_no_public_setters()
    {
        var entities = Domain.GetTypes().Where(IsEntity).ToArray();

        entities.ShouldNotBeEmpty(
            $"this rule is vacuous if it finds no entities - check that '{EntityBaseType}' still "
            + "names the base type, because a rename would make this test pass by finding nothing");

        var offenders = entities
            .SelectMany(entity => entity
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{entity.Name}.{property.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            "an entity's state may only change through a method that also raises the corresponding "
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

    // Value types included deliberately: filtering to classes would silently skip an event declared
    // as a `readonly record struct`, and the non-empty guards above would still pass on the strength
    // of the existing ones.
    private static Type[] DomainEventTypes() =>
        Domain.GetTypes()
            .Where(type => type is { IsPublic: true, IsAbstract: false })
            .Where(ImplementsDomainEvent)
            .ToArray();

    private static bool ImplementsDomainEvent(Type type) =>
        Array.Exists(
            type.GetInterfaces(),
            contract => string.Equals(contract.FullName, DomainEventInterface, StringComparison.Ordinal));

    private static bool IsEntity(Type type)
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
                    EntityBaseType,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

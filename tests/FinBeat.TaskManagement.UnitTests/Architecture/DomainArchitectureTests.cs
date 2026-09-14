using System.Reflection;
using FinBeat.TaskManagement.Domain.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using NetArchTest.Rules;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Pins the structural properties the whole layering rests on: the Domain project must depend on
/// nothing but the base class library, and its aggregate must not leak mutable state.
/// </summary>
/// <remarks>
/// Two complementary techniques are used deliberately. <see cref="SolutionLayout"/> reads the
/// <c>.csproj</c> XML directly, so "Domain declares zero package and project references" is
/// enforced the moment somebody adds one in the IDE, and holds even while a referencing layer is
/// still empty — reflection over a compiled assembly cannot do that, because an unused reference
/// leaves no trace in the compiled output. The remaining checks read the compiled assembly instead,
/// because they are about what the IL actually contains (dependencies, sealed-ness, interface
/// implementation), which only exists once code has been written against it.
/// </remarks>
public sealed class DomainArchitectureTests
{
    private const string DomainProjectName = "FinBeat.TaskManagement.Domain";
    private const string EventsNamespace = "FinBeat.TaskManagement.Domain.Tasks.Events";

    private static readonly Assembly DomainAssembly = typeof(TaskItem).Assembly;

    [Fact]
    public void Domain_project_file_declares_no_project_references()
    {
        var references = SolutionLayout.ProjectReferences(DomainProjectName);

        references.ShouldBeEmpty(
            "the Domain layer is the centre of the architecture and must depend on nothing; "
            + "whatever it needs from the outside belongs behind an interface declared in Domain "
            + $"and implemented further out. Offending references: {string.Join(", ", references)}");
    }

    [Fact]
    public void Domain_project_file_declares_no_package_references()
    {
        var packages = SolutionLayout.PackageReferences(DomainProjectName);

        packages.ShouldBeEmpty(
            "the Domain layer must stay on the base class library alone; a NuGet package here "
            + "would couple the business rules to a third party's release cycle. Offending "
            + $"packages: {string.Join(", ", packages)}");
    }

    [Fact]
    public void Domain_assembly_has_no_dependency_on_ef_core_application_or_infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "FinBeat.TaskManagement.Application",
                "FinBeat.TaskManagement.Infrastructure")
            .GetResult();

        var offenders = result.FailingTypeNames ?? [];

        result.IsSuccessful.ShouldBeTrue(
            "these types reach for EF Core, Application, or Infrastructure directly, which the "
            + $"Domain layer must never do: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Domain_assembly_references_only_the_base_class_library()
    {
        var offenders = DomainAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !IsBaseClassLibrary(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            "the Domain layer must compile against the base class library and nothing else, which "
            + "is what lets the business rules be reasoned about and tested in isolation. "
            + $"Assemblies referenced that are not part of the BCL: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Domain_event_types_are_sealed_and_implement_IDomainEvent()
    {
        var eventTypes = DomainAssembly
            .GetTypes()
            .Where(type => type is { IsPublic: true, IsClass: true } && type.Namespace == EventsNamespace)
            .ToArray();

        eventTypes.ShouldNotBeEmpty(
            $"no public types were found in the '{EventsNamespace}' namespace; check that namespace "
            + "string still matches where the domain events live.");

        var notSealed = eventTypes.Where(type => !type.IsSealed).Select(type => type.Name).ToArray();
        notSealed.ShouldBeEmpty(
            $"these domain event types are not sealed: {string.Join(", ", notSealed)}. Sealing them "
            + "keeps a domain event a closed, serializable fact rather than something a caller can subclass.");

        var notImplementing = eventTypes
            .Where(type => !typeof(IDomainEvent).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToArray();
        notImplementing.ShouldBeEmpty(
            $"these types in the events namespace do not implement {nameof(IDomainEvent)}: "
            + $"{string.Join(", ", notImplementing)}.");
    }

    [Fact]
    public void TaskItem_exposes_no_public_property_setters()
    {
        var offenders = typeof(TaskItem)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            $"these public properties of {nameof(TaskItem)} expose a public setter, which lets a "
            + "caller change aggregate state without going through a method that enforces its "
            + $"invariants and raises the matching domain event: {string.Join(", ", offenders)}.");
    }

    private static bool IsBaseClassLibrary(string assemblyName) =>
        assemblyName.StartsWith("System.", StringComparison.Ordinal)
        || assemblyName is "System" or "netstandard" or "mscorlib";
}

using System.Reflection;
using NetArchTest.Rules;

namespace FinBeat.TaskManagement.ArchitectureTests;

/// <summary>The layer graph, written down once - every architecture rule derives from here.</summary>
/// <remarks>
///   Api --> Infrastructure --> Application --+--> Domain
///                                            |
///   Listener --------------------------------+--> Contracts
/// </remarks>
internal static class ArchitectureModel
{
    internal const string Domain = "FinBeat.TaskManagement.Domain";

    // Kept apart from Domain: domain events hold value objects with private constructors, which
    // serialize but do not deserialize back.
    internal const string Contracts = "FinBeat.TaskManagement.Contracts";

    internal const string Application = "FinBeat.TaskManagement.Application";

    internal const string Infrastructure = "FinBeat.TaskManagement.Infrastructure";

    internal const string Api = "FinBeat.TaskManagement.Api";

    internal const string Listener = "FinBeat.TaskManagement.Listener";

    internal static readonly string[] AllLayers = [Domain, Contracts, Application, Infrastructure, Api, Listener];

    // Hosts excluded: top-level statements put the entry point in the global namespace, so the rule
    // can't be satisfied there.
    internal static readonly string[] LibraryLayers = [Domain, Contracts, Application, Infrastructure];

    // No projects, no packages, BCL only.
    internal static readonly string[] DependencyFreeLayers = [Domain, Contracts];

    // A judgement call, not a consequence of the ordering: Api also takes Infrastructure (composition
    // root binds the adapters), and Listener takes only Contracts (it's a separate deployable - give
    // it Application and it's the same monolith deployed twice, holding a database it never needed).
    internal static readonly IReadOnlyDictionary<string, string[]> RequiredReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Contracts, [] },
            { Application, [Domain, Contracts] },
            { Infrastructure, [Application] },
            { Api, [Application, Infrastructure] },
            { Listener, [Contracts] }
        };

    // Derived rather than a second hand-kept table: a widened row there would quietly shrink this
    // list, and the suite would go greener while getting weaker.
    internal static string[] AllowedReferences(string layer) =>
        RequiredReferences[layer]
            .SelectMany(required => AllowedReferences(required).Prepend(required))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    // An allow-list, not a deny-list, since a deny-list misses whatever nobody thought to ban. Layers
    // absent from the keys are unconstrained. Application gets EF Core alone, for the DbSet on
    // IApplicationDbContext.
    internal static readonly IReadOnlyDictionary<string, string[]> AllowedExternalReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Contracts, [] },
            { Application, ["Microsoft.EntityFrameworkCore"] }
        };

    // Same policy against the compiled assembly: a PackageReference says what was declared, this says
    // what was actually used, transitive references included.
    internal static readonly IReadOnlyDictionary<string, string[]> AllowedExternalAssemblies =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Contracts, [] },
            { Application, ["Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.Abstractions"] }
        };

    internal static string[] ForbiddenFor(string layer)
    {
        var allowed = AllowedReferences(layer);

        return AllLayers
            .Where(other => !string.Equals(other, layer, StringComparison.Ordinal))
            .Where(other => !allowed.Contains(other, StringComparer.Ordinal))
            .ToArray();
    }

    // By name, not typeof(X).Assembly - a layer with no types yet still needs to be checkable.
    internal static Assembly LoadAssembly(string layer) => Assembly.Load(layer);

    // Forbidden names match as namespace prefixes, so a global-namespace type is invisible here -
    // which is why the namespace-convention rule matters too.
    internal static IReadOnlyList<string> TypesDependingOn(string layer, params string[] forbidden) =>
        Types.InAssembly(LoadAssembly(layer))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult()
            .FailingTypes
            .Select(type => string.IsNullOrWhiteSpace(type.Explanation)
                ? type.FullName
                : $"{type.FullName} ({type.Explanation})")
            .ToArray();
}

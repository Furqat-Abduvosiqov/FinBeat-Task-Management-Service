using System.Reflection;
using NetArchTest.Rules;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

// The single place that describes this solution's Clean Architecture layering. Every rule in this
// folder is derived from what is here, so re-shaping the solution is a one-file edit rather than a
// hunt through test methods.
//
//   Api ------+
//             +--> Infrastructure --> Application --> Domain
//   Listener -+
internal static class ArchitectureModel
{
    /// <summary>Enterprise core: entities, value objects, domain events, repository contracts. Depends on nothing.</summary>
    internal const string Domain = "FinBeat.TaskManagement.Domain";

    /// <summary>Use cases orchestrating the domain. Knows the domain; knows nothing of how it is stored or delivered.</summary>
    internal const string Application = "FinBeat.TaskManagement.Application";

    /// <summary>Adapters: persistence, messaging, external services. Implements the contracts the inner layers declare.</summary>
    internal const string Infrastructure = "FinBeat.TaskManagement.Infrastructure";

    /// <summary>HTTP delivery mechanism and composition root.</summary>
    internal const string Api = "FinBeat.TaskManagement.Api";

    /// <summary>Worker delivery mechanism and composition root for the task-event listener service.</summary>
    internal const string Listener = "FinBeat.TaskManagement.Listener";

    /// <summary>Every layer of the solution, innermost first.</summary>
    internal static readonly string[] AllLayers = [Domain, Application, Infrastructure, Api, Listener];

    /// <summary>
    /// The class-library layers. The two hosts are excluded from conventions that top-level
    /// statements make impossible to satisfy — the compiler emits an entry point into the global namespace.
    /// </summary>
    internal static readonly string[] LibraryLayers = [Domain, Application, Infrastructure];

    /// <summary>
    /// The layers that carry business rules and so must stay free of delivery and persistence
    /// technology. Named here rather than in a test attribute so a new inner layer is covered automatically.
    /// </summary>
    internal static readonly string[] BusinessRuleLayers = [Domain, Application];

    /// <summary>
    /// References each project must declare.
    /// </summary>
    /// <remarks>
    /// This is the one table that carries real editorial judgement, because it does not follow from
    /// the layer ordering: both hosts name Application <em>and</em> Infrastructure, skipping a rank,
    /// because a composition root binds the concrete adapters while also calling the use cases directly.
    /// </remarks>
    internal static readonly IReadOnlyDictionary<string, string[]> RequiredReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Application, [Domain] },
            { Infrastructure, [Application] },
            { Api, [Application, Infrastructure] },
            { Listener, [Application, Infrastructure] }
        };

    /// <summary>
    /// References a project is permitted to declare: everything it already reaches transitively.
    /// Naming an inner layer explicitly is a style choice; naming an outer one is a violation.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="RequiredReferences"/> rather than written out. A second hand-kept
    /// table would fail open — widening a row silently removes cases from
    /// <see cref="ForbiddenFor"/>, and a theory that generates fewer cases reports no error, so the
    /// suite would go greener instead of red.
    /// </remarks>
    internal static string[] AllowedReferences(string layer) =>
        RequiredReferences[layer]
            .SelectMany(required => AllowedReferences(required).Prepend(required))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The layers <paramref name="layer"/> must never depend on.</summary>
    internal static string[] ForbiddenFor(string layer)
    {
        var allowed = AllowedReferences(layer);

        return AllLayers
            .Where(other => !string.Equals(other, layer, StringComparison.Ordinal))
            .Where(other => !allowed.Contains(other, StringComparer.Ordinal))
            .ToArray();
    }

    /// <summary>
    /// Loads a layer's compiled assembly by name.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>typeof(SomeType).Assembly</c>: the layers are still being built out, and a
    /// layer with no types yet offers nothing to anchor on. Loading by name keeps these rules working
    /// against an empty layer, which is exactly when a wrong reference is cheapest to correct.
    /// </remarks>
    internal static Assembly LoadAssembly(string layer) => Assembly.Load(layer);

    /// <summary>
    /// The types in <paramref name="layer"/> that depend on any of <paramref name="forbidden"/>,
    /// each annotated with the reason the rule matched it.
    /// </summary>
    /// <remarks>
    /// Note the blind spot this cannot see: a dependency on a type in the global namespace is
    /// invisible to the matcher, because the forbidden names are namespace prefixes and such a type
    /// has no namespace to match against. Keeping every type under its layer namespace is what
    /// closes that hole, which is why the namespace convention is load-bearing rather than cosmetic.
    /// </remarks>
    internal static IReadOnlyList<string> TypesDependingOn(string layer, params string[] forbidden) =>
        (Types.InAssembly(LoadAssembly(layer))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult()
            .FailingTypes ?? [])
        .Select(type => string.IsNullOrWhiteSpace(type.Explanation)
            ? type.FullName
            : $"{type.FullName} ({type.Explanation})")
        .ToArray();
}

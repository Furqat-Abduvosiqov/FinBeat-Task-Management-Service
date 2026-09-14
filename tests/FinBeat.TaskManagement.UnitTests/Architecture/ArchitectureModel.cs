using System.Reflection;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// The single place that describes this solution's Clean Architecture layering.
/// Every rule in this folder is derived from the tables below, so re-shaping the solution is a
/// one-file edit rather than a hunt through test methods.
/// </summary>
/// <remarks>
/// The layering is the usual concentric one, with dependencies pointing strictly inward:
/// <code>
///   Api ─┐
///        ├─&gt; Infrastructure ─&gt; Application ─&gt; Domain
///   Listener ─┘
/// </code>
/// </remarks>
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
    /// The three class-library layers. The two hosts are excluded from conventions that top-level
    /// statements make impossible to satisfy — the compiler emits an entry point into the global namespace.
    /// </summary>
    internal static readonly string[] LibraryLayers = [Domain, Application, Infrastructure];

    /// <summary>
    /// References each project <em>must</em> declare.
    /// </summary>
    /// <remarks>
    /// Both hosts reference Infrastructure because a composition root is the one place allowed to know
    /// the concrete adapters — that is where the DI container binds them to the interfaces the inner
    /// layers declare. Nothing further in should ever do the same.
    /// </remarks>
    internal static readonly IReadOnlyDictionary<string, string[]> RequiredReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Application, [Domain] },
            { Infrastructure, [Application] },
            { Api, [Application, Infrastructure] },
            { Listener, [Application, Infrastructure] },
        };

    /// <summary>
    /// References each project is <em>permitted</em> to declare — a superset of <see cref="RequiredReferences"/>.
    /// </summary>
    /// <remarks>
    /// The extra entries are all inner layers a project already reaches transitively; naming one
    /// explicitly is a style choice, not an architecture violation. Naming an <em>outer</em> layer is,
    /// which is why the two hosts never appear in anyone's list.
    /// </remarks>
    internal static readonly IReadOnlyDictionary<string, string[]> AllowedReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Application, [Domain] },
            { Infrastructure, [Application, Domain] },
            { Api, [Application, Infrastructure, Domain] },
            { Listener, [Application, Infrastructure, Domain] },
        };

    /// <summary>
    /// The layers <paramref name="layer"/> must never depend on, derived from <see cref="AllowedReferences"/>
    /// so the two can never drift apart.
    /// </summary>
    internal static string[] ForbiddenFor(string layer) =>
        AllLayers
            .Where(other => !string.Equals(other, layer, StringComparison.Ordinal))
            .Where(other => !AllowedReferences[layer].Contains(other, StringComparer.Ordinal))
            .ToArray();

    /// <summary>
    /// Loads a layer's compiled assembly by name.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>typeof(SomeType).Assembly</c>: the layers are still being built out, and a
    /// layer with no types yet offers nothing to anchor on. Loading by name keeps these rules working
    /// against an empty layer, which is exactly when a wrong reference is cheapest to correct.
    /// </remarks>
    internal static Assembly LoadAssembly(string layer) => Assembly.Load(new AssemblyName(layer));

    /// <summary>
    /// Renders a failure message that names every offender, so a violation is diagnosable from the
    /// test output alone without re-running anything.
    /// </summary>
    internal static string Describe(string headline, IEnumerable<string> offenders) =>
        headline
        + Environment.NewLine
        + string.Join(Environment.NewLine, offenders.Select(offender => "  - " + offender));
}

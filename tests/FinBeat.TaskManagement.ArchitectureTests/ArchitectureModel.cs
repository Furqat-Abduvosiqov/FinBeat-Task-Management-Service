using System.Reflection;
using NetArchTest.Rules;

namespace FinBeat.TaskManagement.ArchitectureTests;

// The single place that describes this solution's Clean Architecture layering. Every rule in this
// folder is derived from what is here, so re-shaping the solution is a one-file edit rather than a
// hunt through test methods.
//
//   Api --> Infrastructure --> Application --+--> Domain
//                                            |
//   Listener --------------------------------+--> Contracts
//
// Api also names Application directly, and both hosts are composition roots. The Listener is a
// separate deployable and shares only the wire format, never the model.
internal static class ArchitectureModel
{
    internal const string Domain = "FinBeat.TaskManagement.Domain";

    // The published wire format: flat integration-event records shared between the service that
    // emits task-change events and the one that consumes them. Separate from Domain on purpose -
    // domain events carry value objects with private constructors, which serialize but cannot
    // deserialize, and putting the aggregate on the wire would couple two independently deployable
    // services to each other's internals.
    internal const string Contracts = "FinBeat.TaskManagement.Contracts";

    internal const string Application = "FinBeat.TaskManagement.Application";

    internal const string Infrastructure = "FinBeat.TaskManagement.Infrastructure";

    internal const string Api = "FinBeat.TaskManagement.Api";

    internal const string Listener = "FinBeat.TaskManagement.Listener";

    internal static readonly string[] AllLayers = [Domain, Contracts, Application, Infrastructure, Api, Listener];

    // The hosts are excluded from conventions that top-level statements make impossible to satisfy:
    // the compiler emits an entry point into the global namespace.
    internal static readonly string[] LibraryLayers = [Domain, Contracts, Application, Infrastructure];

    // Must depend on nothing at all - no projects, no packages, base class library only. Domain
    // because it is the centre of the architecture; Contracts because anything a published wire
    // format references becomes a versioning obligation for every service that consumes it.
    internal static readonly string[] DependencyFreeLayers = [Domain, Contracts];

    // The one table carrying real editorial judgement, because it does not follow from the layer
    // ordering. Two rows in particular:
    //
    //   Api      names Application AND Infrastructure, skipping a rank, because a composition root
    //            binds the concrete adapters while also calling the use cases directly.
    //   Listener names ONLY Contracts. It is a separate deployable whose job is to receive
    //            task-change events and log them, so the wire format is the entire legitimate
    //            overlap. Handing it Application or Infrastructure would give a log-only service the
    //            aggregate, the repository and the production database, and would make "separate
    //            service" a naming convention rather than a fact the build enforces.
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

    // What a project may declare: everything it already reaches transitively. Naming an inner layer
    // explicitly is a style choice; naming an outer one is a violation.
    //
    // Derived rather than written out, because a second hand-kept table would fail OPEN - widening a
    // row silently removes cases from ForbiddenFor, and a theory that generates fewer cases reports
    // no error, so the suite would go greener instead of red.
    internal static string[] AllowedReferences(string layer) =>
        RequiredReferences[layer]
            .SelectMany(required => AllowedReferences(required).Prepend(required))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    // Third-party code each inner layer may pull in, by NuGet package id or shared-framework name.
    // Empty means none.
    //
    // An allow-list, not a deny-list, and that is the whole point: a deny-list passes anything nobody
    // thought to forbid, so MongoDB.Driver or StackExchange.Redis would land in Application with a
    // green suite. Nobody adding a package thinks to go edit a list of banned ones. Under an
    // allow-list, adding a package means editing this table - which is the conversation the rule
    // exists to force. The keys are also the definition of which layers are constrained at all;
    // Infrastructure and the hosts are absent because adapters and composition roots are exactly
    // where technology is supposed to live.
    internal static readonly IReadOnlyDictionary<string, string[]> AllowedExternalReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Contracts, [] },
            { Application, [] }
        };

    internal static string[] ForbiddenFor(string layer)
    {
        var allowed = AllowedReferences(layer);

        return AllLayers
            .Where(other => !string.Equals(other, layer, StringComparison.Ordinal))
            .Where(other => !allowed.Contains(other, StringComparer.Ordinal))
            .ToArray();
    }

    // Deliberately not typeof(SomeType).Assembly: the layers are still being built out, and a layer
    // with no types yet offers nothing to anchor on. Loading by name keeps these rules working
    // against an empty layer, which is exactly when a wrong reference is cheapest to correct.
    internal static Assembly LoadAssembly(string layer) => Assembly.Load(layer);

    // The types in a layer that depend on any of the forbidden names, annotated with the reason each
    // matched.
    //
    // The blind spot this cannot see: a dependency on a type in the global namespace is invisible to
    // the matcher, because the forbidden names are namespace prefixes and such a type has none to
    // match against. Keeping every type under its layer namespace is what closes that hole, which is
    // why the namespace convention is load-bearing rather than cosmetic.
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

using System.Reflection;
using NetArchTest.Rules;

namespace FinBeat.TaskManagement.ArchitectureTests;

// The architecture, written down once. Every rule derives from here. See README.md for the reasoning.
//
//   Api --> Infrastructure --> Application --+--> Domain
//                                            |
//   Listener --------------------------------+--> Contracts
internal static class ArchitectureModel
{
    internal const string Domain = "FinBeat.TaskManagement.Domain";

    // The wire format. Kept apart from Domain because domain events hold value objects with private
    // constructors: they serialize, but they will not come back.
    internal const string Contracts = "FinBeat.TaskManagement.Contracts";

    internal const string Application = "FinBeat.TaskManagement.Application";

    internal const string Infrastructure = "FinBeat.TaskManagement.Infrastructure";

    internal const string Api = "FinBeat.TaskManagement.Api";

    internal const string Listener = "FinBeat.TaskManagement.Listener";

    internal static readonly string[] AllLayers = [Domain, Contracts, Application, Infrastructure, Api, Listener];

    // Hosts are left out: top-level statements put their entry point in the global namespace, so the
    // rule is not satisfiable there.
    internal static readonly string[] LibraryLayers = [Domain, Contracts, Application, Infrastructure];

    // No projects, no packages, BCL only.
    internal static readonly string[] DependencyFreeLayers = [Domain, Contracts];

    // The one table that is a judgement call rather than a consequence of the ordering:
    //   Api      takes Infrastructure too, because a composition root binds the adapters.
    //   Listener takes only Contracts. It is a separate deployable; give it Application and it
    //            becomes the same monolith deployed twice, holding the database it never needed.
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

    // Everything a layer already reaches transitively. Derived, not a second table: widening a hand-
    // kept row would generate fewer cases, and fewer cases is not an error - the suite would go
    // greener while getting weaker.
    internal static string[] AllowedReferences(string layer) =>
        RequiredReferences[layer]
            .SelectMany(required => AllowedReferences(required).Prepend(required))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    // Packages the inner layers may use. An allow-list, because a deny-list misses whatever nobody
    // thought to ban. Layers absent from the keys are unconstrained, which is where technology belongs.
    // Application gets EF Core alone, for the DbSet IApplicationDbContext exposes, and nothing more.
    internal static readonly IReadOnlyDictionary<string, string[]> AllowedExternalReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { Domain, [] },
            { Contracts, [] },
            { Application, ["Microsoft.EntityFrameworkCore"] }
        };

    // The same policy against the compiled assembly. A PackageReference says what was declared; this
    // says what was used, including anything that arrived transitively. Assembly names, not package
    // ids: one package can ship several.
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

    // By name, not typeof(X).Assembly - a layer with no types yet has nothing to anchor on, and that
    // is exactly when a wrong reference is cheapest to fix.
    internal static Assembly LoadAssembly(string layer) => Assembly.Load(layer);

    // Blind spot worth knowing: forbidden names are matched as namespace prefixes, so a dependency on
    // a global-namespace type is invisible here. That is what makes the namespace rule load-bearing.
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

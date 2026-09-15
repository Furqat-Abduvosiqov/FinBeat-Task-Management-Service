using FinBeat.TaskManagement.ArchitectureTests.Internals;
using Shouldly;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// Keeps technology out of the inner layers: an ORM arrives as a package, not a project reference, so
// the layering rules alone can't catch it. Two rules here, checking the project file and the
// compiled assembly respectively, since each catches a different mistake.
public sealed class LayerPurityTests
{
    private static readonly string SharedFrameworkDirectory =
        Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;

    [Theory]
    [MemberData(nameof(ArchitectureData.ExternallyConstrainedLayers), MemberType = typeof(ArchitectureData))]
    public void Inner_layer_declares_no_unapproved_third_party_reference(string layer)
    {
        var approved = ArchitectureModel.AllowedExternalReferences[layer];

        var offenders = SolutionLayout.PackageReferences(layer).Select(id => (Kind: "package", Id: id))
            .Concat(SolutionLayout.FrameworkReferences(layer).Select(id => (Kind: "framework", Id: id)))
            .Where(reference => !approved.Contains(reference.Id, StringComparer.OrdinalIgnoreCase))
            .Select(reference => $"{reference.Kind} {reference.Id}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            $"'{layer}' may declare only approved third-party code. Approved here: "
            + (approved.Length == 0 ? "nothing" : string.Join(", ", approved))
            + ". If this one belongs, add it to ArchitectureModel.AllowedExternalReferences");
    }

    // Closes the hole above: the project file can't show an assembly that arrived transitively, or
    // one an SDK hands over with no XML at all (Microsoft.NET.Sdk.Web brings all of ASP.NET Core).
    [Theory]
    [MemberData(nameof(ArchitectureData.ExternallyConstrainedLayers), MemberType = typeof(ArchitectureData))]
    public void Inner_layer_compiles_against_no_unapproved_assembly(string layer)
    {
        var approved = ArchitectureModel.AllowedExternalAssemblies[layer];
        var reachableLayers = ArchitectureModel.AllowedReferences(layer);

        var offenders = ArchitectureModel.LoadAssembly(layer)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !IsBaseClassLibrary(name))
            .Where(name => !reachableLayers.Contains(name, StringComparer.Ordinal))
            .Where(name => !approved.Contains(name, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            $"'{layer}' compiles against something it is not allowed to use. Beyond the base class "
            + $"library and the layers it may reference, it may use: "
            + (approved.Length == 0 ? "nothing" : string.Join(", ", approved))
            + ". If this one belongs, add it to ArchitectureModel.AllowedExternalAssemblies");
    }

    // Asks where the runtime actually loaded from rather than matching a "System." prefix, which is a
    // poor proxy - System.Data.SqlClient and System.Reactive are ordinary NuGet packages.
    private static bool IsBaseClassLibrary(string assemblyName) =>
        File.Exists(Path.Combine(SharedFrameworkDirectory, assemblyName + ".dll"));
}

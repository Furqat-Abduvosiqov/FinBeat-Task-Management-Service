using FinBeat.TaskManagement.ArchitectureTests.Internals;
using Shouldly;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// Keeps technology out of the inner layers. The layering rules cannot do this on their own: an ORM
// or a broker arrives as a package, not a project reference.
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
            $"'{layer}' may use only approved third-party code. Approved here: "
            + (approved.Length == 0 ? "nothing" : string.Join(", ", approved))
            + ". If this one belongs, add it to ArchitectureModel.AllowedExternalReferences");
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.DependencyFreeLayers), MemberType = typeof(ArchitectureData))]
    public void Dependency_free_layer_references_only_the_base_class_library(string layer)
    {
        var offenders = ArchitectureModel.LoadAssembly(layer)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !IsBaseClassLibrary(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty(
            $"'{layer}' must compile against the base class library and nothing else - that is what "
            + "lets it be reasoned about, tested and versioned in isolation");
    }

    // Asks where the runtime actually loaded from rather than matching a "System." prefix, which is a
    // poor proxy - System.Data.SqlClient and System.Reactive are ordinary NuGet packages.
    private static bool IsBaseClassLibrary(string assemblyName) =>
        File.Exists(Path.Combine(SharedFrameworkDirectory, assemblyName + ".dll"));
}

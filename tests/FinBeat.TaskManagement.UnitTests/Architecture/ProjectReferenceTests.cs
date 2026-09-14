using FluentAssertions;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Rules over the dependency graph the .csproj files declare.
/// </summary>
/// <remarks>
/// These read the project files rather than the compiled assemblies, and so they hold even while a
/// layer is still empty: a forbidden ProjectReference fails here the moment somebody adds it in the
/// IDE, long before any code exists to make the compiler record it.
/// </remarks>
public sealed class ProjectReferenceTests
{
    [Fact]
    public void Every_layer_resolves_to_an_existing_project_file()
    {
        // Regression guard: project discovery once globbed the directory tree, which breaks the
        // moment a second checkout of the repository sits inside it - a git worktree created under
        // the repository root does exactly that. Resolving through the solution file fixed it; this
        // keeps a renamed or moved project from silently resolving to nothing.
        var missing = ArchitectureModel.AllLayers
            .Select(layer => (Layer: layer, File: SolutionLayout.ProjectFile(layer)))
            .Where(entry => !entry.File.Exists)
            .Select(entry => $"{entry.Layer} -> {entry.File.FullName}")
            .ToArray();

        missing.Should().BeEmpty(
            "every layer must resolve to a project file that exists on disk, as listed in {0}",
            SolutionLayout.SolutionFile.Name);
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.AllLayers), MemberType = typeof(ArchitectureData))]
    public void Layer_declares_every_reference_the_architecture_requires(string layer)
    {
        var declared = SolutionLayout.ProjectReferences(layer);
        var missing = ArchitectureModel.RequiredReferences[layer]
            .Where(required => !declared.Contains(required, StringComparer.Ordinal))
            .OrderBy(required => required, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "'{0}' must declare the project references its layer requires - add them to {1}",
            layer,
            SolutionLayout.ProjectFile(layer).Name);
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.AllLayers), MemberType = typeof(ArchitectureData))]
    public void Layer_declares_no_reference_the_architecture_forbids(string layer)
    {
        var allowed = ArchitectureModel.AllowedReferences(layer);
        var violations = SolutionLayout.ProjectReferences(layer)
            .Where(declared => !allowed.Contains(declared, StringComparer.Ordinal))
            .ToArray();

        violations.Should().BeEmpty(
            "dependencies point inward only, so a reference out of '{0}' has to be inverted - declare "
            + "an interface in the inner layer and implement it in the outer one. '{0}' may reference: {1}",
            layer,
            allowed.Length == 0 ? "nothing" : string.Join(", ", allowed));
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.DependencyFreeLayers), MemberType = typeof(ArchitectureData))]
    public void Dependency_free_layer_declares_no_project_references(string layer)
    {
        SolutionLayout.ProjectReferences(layer).Should().BeEmpty(
            "'{0}' must depend on nothing - whatever it needs from the outside belongs behind an "
            + "interface it declares itself and something further out implements",
            layer);
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.DependencyFreeLayers), MemberType = typeof(ArchitectureData))]
    public void Dependency_free_layer_declares_no_package_references(string layer)
    {
        SolutionLayout.PackageReferences(layer).Should().BeEmpty(
            "'{0}' must stay on the base class library alone - a NuGet package here would couple it "
            + "to a third party's release cycle, and for a published wire format that becomes a "
            + "versioning obligation for every service that consumes it",
            layer);
    }
}

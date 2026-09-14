using FinBeat.TaskManagement.ArchitectureTests.Internals;
using Shouldly;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// The graph as declared. These still work on an empty layer: a bad reference fails here the moment
// someone adds it in the IDE, long before any code exists for the compiler to notice.
public sealed class ProjectReferenceTests
{
    [Theory]
    [MemberData(nameof(ArchitectureData.AllLayers), MemberType = typeof(ArchitectureData))]
    public void Layer_declares_every_reference_the_architecture_requires(string layer)
    {
        var declared = SolutionLayout.ProjectReferences(layer);
        var missing = ArchitectureModel.RequiredReferences[layer]
            .Where(required => !declared.Contains(required, StringComparer.Ordinal))
            .OrderBy(required => required, StringComparer.Ordinal)
            .ToArray();

        missing.ShouldBeEmpty(
            $"'{layer}' must declare the project references its layer requires - add them to "
            + SolutionLayout.ProjectFile(layer).Name);
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.AllLayers), MemberType = typeof(ArchitectureData))]
    public void Layer_declares_no_reference_the_architecture_forbids(string layer)
    {
        var allowed = ArchitectureModel.AllowedReferences(layer);
        var violations = SolutionLayout.ProjectReferences(layer)
            .Where(declared => !allowed.Contains(declared, StringComparer.Ordinal))
            .ToArray();

        violations.ShouldBeEmpty(
            "dependencies point inward only, so this one has to be inverted - put the interface in "
            + $"the inner layer and implement it further out. '{layer}' may reference: "
            + (allowed.Length == 0 ? "nothing" : string.Join(", ", allowed)));
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.DependencyFreeLayers), MemberType = typeof(ArchitectureData))]
    public void Dependency_free_layer_declares_no_project_references(string layer)
    {
        SolutionLayout.ProjectReferences(layer).ShouldBeEmpty(
            $"'{layer}' depends on nothing - what it needs from outside goes behind an interface it "
            + "owns");
    }
}

using FluentAssertions;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Rules over what the compiled assemblies actually depend on, type by type.
/// </summary>
/// <remarks>
/// <see cref="ProjectReferenceTests"/> pins the graph the build <em>declares</em>; these pin the graph
/// the IL really contains. The two catch different mistakes: a project reference can be declared and
/// never used, and a dependency can arrive transitively without ever being declared. Both must hold.
/// </remarks>
public sealed class LayerDependencyTests
{
    [Theory]
    [MemberData(nameof(ArchitectureData.ForbiddenEdges), MemberType = typeof(ArchitectureData))]
    public void Layer_has_no_compiled_dependency_on_a_layer_further_out(string layer, string forbidden)
    {
        ArchitectureModel.TypesDependingOn(layer, forbidden).Should().BeEmpty(
            "'{0}' must not depend on '{1}', which sits further out in the architecture - invert it "
            + "by declaring the contract in the inner layer and implementing it in the outer one",
            layer,
            forbidden);
    }
}

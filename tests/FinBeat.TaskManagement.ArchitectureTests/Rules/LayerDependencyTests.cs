using FinBeat.TaskManagement.ArchitectureTests.Internals;
using FluentAssertions;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// The graph as compiled. ProjectReferenceTests catches what is declared and unused; this catches the
// reverse - a dependency that arrives transitively and was never declared anywhere.
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

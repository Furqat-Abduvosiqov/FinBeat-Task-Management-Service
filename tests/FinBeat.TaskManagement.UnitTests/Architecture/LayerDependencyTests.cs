using NetArchTest.Rules;

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
    [MemberData(nameof(ArchitectureData.AllLayers), MemberType = typeof(ArchitectureData))]
    public void Layer_assembly_can_be_loaded_and_inspected(string layer)
    {
        var assembly = ArchitectureModel.LoadAssembly(layer);

        // Without a file on disk the rules below would silently inspect nothing and pass vacuously.
        Assert.False(
            string.IsNullOrEmpty(assembly.Location),
            $"'{layer}' was loaded without a file location, so the architecture rules cannot read its IL.");
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.ForbiddenEdges), MemberType = typeof(ArchitectureData))]
    public void Layer_has_no_compiled_dependency_on_a_layer_further_out(string layer, string forbidden)
    {
        var result = Types.InAssembly(ArchitectureModel.LoadAssembly(layer))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            ArchitectureModel.Describe(
                $"'{layer}' depends on '{forbidden}', which sits further out in the architecture. "
                + "Invert the dependency: declare the contract in the inner layer and implement it in "
                + "the outer one. Offending types:",
                result.FailingTypeNames ?? []));
    }
}

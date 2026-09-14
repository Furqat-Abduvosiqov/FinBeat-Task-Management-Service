namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Theory data shared by the architecture rules, projected from <see cref="ArchitectureModel"/> so
/// the test cases and the layering they check can never drift apart.
/// </summary>
public static class ArchitectureData
{
    public static TheoryData<string> AllLayers => new(ArchitectureModel.AllLayers);

    public static TheoryData<string> LibraryLayers => new(ArchitectureModel.LibraryLayers);

    public static TheoryData<string> BusinessRuleLayers => new(ArchitectureModel.BusinessRuleLayers);

    public static TheoryData<string> DependencyFreeLayers => new(ArchitectureModel.DependencyFreeLayers);

    /// <summary>
    /// One case per forbidden (layer, dependency) edge, so a failure names both ends of the edge
    /// rather than reporting "this layer depends on something it shouldn't".
    /// </summary>
    public static TheoryData<string, string> ForbiddenEdges
    {
        get
        {
            // TheoryData<T1, T2> has no collection initializer taking a sequence, so this stays a loop.
            var data = new TheoryData<string, string>();

            foreach (var layer in ArchitectureModel.AllLayers)
            {
                foreach (var forbidden in ArchitectureModel.ForbiddenFor(layer))
                {
                    data.Add(layer, forbidden);
                }
            }

            return data;
        }
    }
}

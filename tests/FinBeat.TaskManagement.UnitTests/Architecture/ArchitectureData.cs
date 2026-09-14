namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Theory data shared by the architecture rules, projected from <see cref="ArchitectureModel"/> so
/// the test cases and the layering they check can never drift apart.
/// </summary>
public static class ArchitectureData
{
    /// <summary>One case per layer.</summary>
    public static TheoryData<string> AllLayers => ToData(ArchitectureModel.AllLayers);

    /// <summary>One case per class-library layer, excluding the two executable hosts.</summary>
    public static TheoryData<string> LibraryLayers => ToData(ArchitectureModel.LibraryLayers);

    /// <summary>
    /// One case per forbidden (layer, dependency) edge, so a failure names both ends of the edge
    /// rather than reporting "this layer depends on something it shouldn't".
    /// </summary>
    public static TheoryData<string, string> ForbiddenEdges
    {
        get
        {
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

    private static TheoryData<string> ToData(IEnumerable<string> values)
    {
        var data = new TheoryData<string>();

        foreach (var value in values)
        {
            data.Add(value);
        }

        return data;
    }
}

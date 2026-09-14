namespace FinBeat.TaskManagement.ArchitectureTests.Internals;

// Projects the model into what [MemberData] wants, so cases and layering cannot drift apart.
public static class ArchitectureData
{
    public static TheoryData<string> AllLayers => new(ArchitectureModel.AllLayers);

    public static TheoryData<string> LibraryLayers => new(ArchitectureModel.LibraryLayers);

    public static TheoryData<string> ExternallyConstrainedLayers => new(ArchitectureModel.AllowedExternalReferences.Keys);

    public static TheoryData<string> DependencyFreeLayers => new(ArchitectureModel.DependencyFreeLayers);

    // One case per forbidden edge, so a failure names both ends rather than just the guilty layer.
    public static TheoryData<string, string> ForbiddenEdges
    {
        get
        {
            // TheoryData<T1, T2> takes no sequence, so this stays a loop.
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

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Rules over the dependency graph the <c>.csproj</c> files declare.
/// </summary>
/// <remarks>
/// These read the project files rather than the compiled assemblies, and so they hold even while a
/// layer is still empty: a forbidden <c>&lt;ProjectReference&gt;</c> fails here the moment somebody
/// adds it in the IDE, long before any code exists to make the compiler record it.
/// </remarks>
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

        Assert.True(
            missing.Length == 0,
            ArchitectureModel.Describe(
                $"'{layer}' is missing project references its layer requires. Add them to "
                + $"{SolutionLayout.ProjectFile(layer).Name}:",
                missing));
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.AllLayers), MemberType = typeof(ArchitectureData))]
    public void Layer_declares_no_reference_the_architecture_forbids(string layer)
    {
        var allowed = ArchitectureModel.AllowedReferences[layer];
        var violations = SolutionLayout.ProjectReferences(layer)
            .Where(declared => !allowed.Contains(declared, StringComparer.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            ArchitectureModel.Describe(
                $"'{layer}' declares project references its layer is not allowed to have. "
                + "Dependencies point inward only, so this one has to be inverted: declare an interface "
                + "in the inner layer and implement it in the outer one. "
                + $"Allowed here: {FormatAllowed(allowed)}. Offending references:",
                violations));
    }

    [Fact]
    public void Domain_declares_no_project_references()
    {
        var references = SolutionLayout.ProjectReferences(ArchitectureModel.Domain);

        Assert.True(
            references.Count == 0,
            ArchitectureModel.Describe(
                "The Domain layer is the centre of the architecture and must depend on nothing. "
                + "Whatever it needs from the outside belongs behind an interface declared in Domain "
                + "and implemented further out. Offending references:",
                references));
    }

    [Fact]
    public void Domain_declares_no_package_references()
    {
        var packages = SolutionLayout.PackageReferences(ArchitectureModel.Domain);

        Assert.True(
            packages.Count == 0,
            ArchitectureModel.Describe(
                "The Domain layer must stay on the base class library alone — no NuGet packages. "
                + "A package here would couple the business rules to a third party's release cycle. "
                + "Offending packages:",
                packages));
    }

    private static string FormatAllowed(IReadOnlyCollection<string> allowed) =>
        allowed.Count == 0 ? "nothing" : string.Join(", ", allowed);
}

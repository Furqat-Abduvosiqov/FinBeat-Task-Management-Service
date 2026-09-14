using FluentAssertions;

namespace FinBeat.TaskManagement.ArchitectureTests;

// Keeps third-party technology out of the inner layers. Layer ordering alone does not do this: an
// ORM or a message broker arrives as a package or a framework reference, not a project reference,
// so it slips past the layering rules entirely. Keeping it out is what makes the business rules
// testable without a database and re-hostable behind a different transport.
public sealed class LayerPurityTests
{
    // Where the runtime actually loaded the shared framework from. Membership of that directory is
    // what "part of the base class library" means - see IsBaseClassLibrary.
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

        offenders.Should().BeEmpty(
            "'{0}' may pull in only approved third-party code. This is an allow-list on purpose: a "
            + "deny-list passes whatever nobody thought to forbid, and nobody adding a package goes "
            + "looking for a list of banned ones. Approved here: {1}. To add one, add it to "
            + "ArchitectureModel.AllowedExternalReferences - that edit is the conversation this rule "
            + "exists to force",
            layer,
            approved.Length == 0 ? "nothing" : string.Join(", ", approved));
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

        offenders.Should().BeEmpty(
            "'{0}' must compile against the base class library and nothing else - that is what lets "
            + "it be reasoned about, tested and versioned in isolation",
            layer);
    }

    // Membership is tested by asking where the runtime actually loaded from, not by matching a
    // "System." name prefix. The prefix is a poor proxy: System.Data.SqlClient, System.Reactive and
    // System.IdentityModel.Tokens.Jwt are all ordinary NuGet packages that would pass it. Asking the
    // directory also covers the netstandard and mscorlib facades without naming them, since both are
    // files sitting in it.
    private static bool IsBaseClassLibrary(string assemblyName) =>
        File.Exists(Path.Combine(SharedFrameworkDirectory, assemblyName + ".dll"));
}

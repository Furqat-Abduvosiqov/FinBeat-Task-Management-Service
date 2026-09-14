using System.Runtime.CompilerServices;
using FinBeat.TaskManagement.ArchitectureTests.Internals;
using FluentAssertions;

namespace FinBeat.TaskManagement.ArchitectureTests.Rules;

// Namespace and assembly name stay in step, so a namespace is a reliable answer to "which layer is
// this?" - which every other rule here assumes. Hosts are exempt; top-level statements put their
// entry point in the global namespace.
public sealed class NamespaceConventionTests
{
    [Theory]
    [MemberData(nameof(ArchitectureData.LibraryLayers), MemberType = typeof(ArchitectureData))]
    public void Library_layer_types_reside_under_the_layer_root_namespace(string layer)
    {
        var offenders = ArchitectureModel.LoadAssembly(layer)
            .GetTypes()
            .Where(type => !type.IsNested)
            // Embedded attributes like NullableAttribute land outside the namespace and are not ours.
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(type => !ResidesUnder(type.Namespace, layer))
            .Select(type => $"{type.FullName ?? type.Name} (namespace: {type.Namespace ?? "<global>"})")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.Should().BeEmpty(
            "every type in '{0}' belongs under the '{0}' namespace - the other rules read namespaces "
            + "to tell layers apart, so a type filed elsewhere is invisible to them",
            layer);
    }

    // Not NetArchTest's ResideInNamespaceStartingWith: that is a raw prefix match, so a type in
    // FinBeat.TaskManagement.DomainHelpers would pass as Domain.
    private static bool ResidesUnder(string? candidate, string rootNamespace) =>
        candidate is not null
        && (string.Equals(candidate, rootNamespace, StringComparison.Ordinal)
            || candidate.StartsWith(rootNamespace + ".", StringComparison.Ordinal));
}

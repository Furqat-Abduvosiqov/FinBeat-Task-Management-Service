using System.Runtime.CompilerServices;
using FluentAssertions;

namespace FinBeat.TaskManagement.ArchitectureTests;

// Keeps each layer's namespace and its assembly name in step, so a type's namespace is a reliable
// statement about which layer it belongs to - which is what every other rule in this folder assumes.
//
// Only the class-library layers are checked. The two hosts use top-level statements, and the
// compiler necessarily emits their entry point into the global namespace, so the rule is not
// satisfiable there as written.
public sealed class NamespaceConventionTests
{
    [Theory]
    [MemberData(nameof(ArchitectureData.LibraryLayers), MemberType = typeof(ArchitectureData))]
    public void Library_layer_types_reside_under_the_layer_root_namespace(string layer)
    {
        var offenders = ArchitectureModel.LoadAssembly(layer)
            .GetTypes()
            .Where(type => !type.IsNested)
            // The compiler embeds attributes such as NullableAttribute into every assembly that uses
            // nullable annotations; they land outside the layer's namespace and are not ours to move.
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(type => !ResidesUnder(type.Namespace, layer))
            .Select(type => $"{type.FullName ?? type.Name} (namespace: {type.Namespace ?? "<global>"})")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        offenders.Should().BeEmpty(
            "every type in '{0}' must sit under the '{0}' namespace - the architecture rules read "
            + "namespaces to decide which layer a type belongs to, so a type filed elsewhere is "
            + "invisible to them",
            layer);
    }

    // Deliberately not NetArchTest's ResideInNamespaceStartingWith: that is a raw prefix match with
    // no separator check, so a type in FinBeat.TaskManagement.DomainHelpers would satisfy the rule
    // for the FinBeat.TaskManagement.Domain layer.
    private static bool ResidesUnder(string? candidate, string rootNamespace) =>
        candidate is not null
        && (string.Equals(candidate, rootNamespace, StringComparison.Ordinal)
            || candidate.StartsWith(rootNamespace + ".", StringComparison.Ordinal));
}

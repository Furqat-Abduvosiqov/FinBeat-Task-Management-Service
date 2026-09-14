using System.Reflection;
using System.Runtime.CompilerServices;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Keeps each layer's namespace and its assembly name in step, so a type's namespace is a reliable
/// statement about which layer it belongs to — which is what every other rule in this folder assumes.
/// </summary>
/// <remarks>
/// Only the class-library layers are checked. The two hosts use top-level statements, and the
/// compiler necessarily emits their entry point into the global namespace, so the rule is not
/// satisfiable there.
/// </remarks>
public sealed class NamespaceConventionTests
{
    [Theory]
    [MemberData(nameof(ArchitectureData.LibraryLayers), MemberType = typeof(ArchitectureData))]
    public void Library_layer_types_reside_under_the_layer_root_namespace(string layer)
    {
        var offenders = GetDefinedTypes(ArchitectureModel.LoadAssembly(layer))
            .Where(type => !type.IsNested)
            // The compiler embeds attributes such as NullableAttribute into every assembly that uses
            // nullable annotations; they land outside the layer's namespace and are not ours to move.
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(type => !ResidesUnder(type.Namespace, layer))
            .Select(type => $"{type.FullName ?? type.Name} (namespace: {type.Namespace ?? "<global>"})")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            ArchitectureModel.Describe(
                $"Every type in '{layer}' must sit under the '{layer}' namespace — the architecture "
                + "rules read namespaces to decide which layer a type belongs to, so a type filed "
                + "elsewhere is invisible to them. Offending types:",
                offenders));
    }

    private static bool ResidesUnder(string? candidate, string rootNamespace) =>
        candidate is not null
        && (string.Equals(candidate, rootNamespace, StringComparison.Ordinal)
            || candidate.StartsWith(rootNamespace + ".", StringComparison.Ordinal));

    /// <summary>
    /// Reads an assembly's types, degrading to the types that did load if some could not be resolved,
    /// so a missing dependency surfaces as a named violation rather than an opaque exception.
    /// </summary>
    private static IEnumerable<Type> GetDefinedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}

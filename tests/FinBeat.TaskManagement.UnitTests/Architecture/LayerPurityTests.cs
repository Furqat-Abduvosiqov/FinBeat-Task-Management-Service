using NetArchTest.Rules;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Rules that keep the two business-rule layers free of delivery and persistence technology.
/// </summary>
/// <remarks>
/// Layer ordering alone does not stop an ORM or a message broker from being referenced directly by
/// Domain or Application — those come in as NuGet packages, not project references, so they slip past
/// the layering rules entirely. Keeping them out is what makes the business rules testable without a
/// database and re-hostable behind a different transport.
/// </remarks>
public sealed class LayerPurityTests
{
    /// <summary>
    /// Namespaces that belong to an adapter. Infrastructure is the layer that may reference these;
    /// the hosts may too, since a composition root has to name the adapters it wires up.
    /// </summary>
    private static readonly string[] AdapterTechnologies =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.SqlClient",
        "System.Data.SqlClient",
        "Npgsql",
        "Dapper",
        "MassTransit",
        "RabbitMQ",
        "Confluent.Kafka",
        "Microsoft.AspNetCore",
        "Swashbuckle",
    ];

    [Fact]
    public void Domain_assembly_references_only_the_base_class_library()
    {
        var offenders = ArchitectureModel.LoadAssembly(ArchitectureModel.Domain)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !IsBaseClassLibrary(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            ArchitectureModel.Describe(
                "The Domain layer must compile against the base class library and nothing else — that "
                + "is what lets the business rules be reasoned about and tested in isolation. "
                + "Assemblies referenced that are not part of the BCL:",
                offenders));
    }

    [Theory]
    [InlineData(ArchitectureModel.Domain)]
    [InlineData(ArchitectureModel.Application)]
    public void Business_rule_layer_has_no_dependency_on_persistence_messaging_or_web_technology(string layer)
    {
        var result = Types.InAssembly(ArchitectureModel.LoadAssembly(layer))
            .ShouldNot()
            .HaveDependencyOnAny(AdapterTechnologies)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            ArchitectureModel.Describe(
                $"'{layer}' reaches for a persistence, messaging or web technology directly. Those "
                + "belong in Infrastructure, behind an interface this layer owns. Offending types:",
                result.FailingTypeNames ?? []));
    }

    private static bool IsBaseClassLibrary(string assemblyName) =>
        assemblyName.StartsWith("System.", StringComparison.Ordinal)
        || assemblyName is "System" or "netstandard" or "mscorlib";
}

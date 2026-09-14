using FluentAssertions;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Rules that keep the business-rule layers free of delivery and persistence technology.
/// </summary>
/// <remarks>
/// Layer ordering alone does not stop an ORM or a message broker from being referenced directly by
/// Domain or Application — those arrive as NuGet packages, not project references, so they slip past
/// the layering rules entirely. Keeping them out is what makes the business rules testable without a
/// database and re-hostable behind a different transport.
/// </remarks>
public sealed class LayerPurityTests
{
    /// <summary>
    /// The directory the shared framework was loaded from. Membership of it is what "part of the
    /// base class library" actually means — see <see cref="IsBaseClassLibrary"/>.
    /// </summary>
    private static readonly string SharedFrameworkDirectory =
        Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;

    /// <summary>
    /// Namespaces that belong to an adapter. Infrastructure is the layer that may reference these;
    /// the hosts may too, since a composition root has to name the adapters it wires up.
    /// </summary>
    /// <remarks>
    /// This is a deny-list, so an unlisted technology passes silently. It is a backstop, not the
    /// primary defence: for Domain, <see cref="Domain_assembly_references_only_the_base_class_library"/>
    /// is the allow-list that actually closes the door. Application has no equivalent yet because it
    /// is expected to take on libraries such as a mediator or a validator.
    /// </remarks>
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

        offenders.Should().BeEmpty(
            "the Domain layer must compile against the base class library and nothing else - that is "
            + "what lets the business rules be reasoned about and tested in isolation");
    }

    [Theory]
    [MemberData(nameof(ArchitectureData.BusinessRuleLayers), MemberType = typeof(ArchitectureData))]
    public void Business_rule_layer_has_no_dependency_on_persistence_messaging_or_web_technology(string layer)
    {
        ArchitectureModel.TypesDependingOn(layer, AdapterTechnologies).Should().BeEmpty(
            "'{0}' must not reach for a persistence, messaging or web technology directly - those "
            + "belong in Infrastructure, behind an interface this layer owns",
            layer);
    }

    /// <summary>
    /// Whether <paramref name="assemblyName"/> ships in the shared framework.
    /// </summary>
    /// <remarks>
    /// Membership is tested by asking where the runtime actually loaded from, not by matching a
    /// <c>System.</c> name prefix. The prefix is a poor proxy: <c>System.Data.SqlClient</c>,
    /// <c>System.Reactive</c> and <c>System.IdentityModel.Tokens.Jwt</c> are all ordinary NuGet
    /// packages that would pass it. This also covers the <c>netstandard</c> and <c>mscorlib</c>
    /// facades without naming them, because both are files in that same directory.
    /// </remarks>
    private static bool IsBaseClassLibrary(string assemblyName) =>
        File.Exists(Path.Combine(SharedFrameworkDirectory, assemblyName + ".dll"));
}

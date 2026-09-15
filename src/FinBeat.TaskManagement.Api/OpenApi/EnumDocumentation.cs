using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace FinBeat.TaskManagement.Api.OpenApi;

/// <summary>Describes an enum as the numbers it travels as, naming what each one means.</summary>
/// <remarks>Meanings go in the schema description, read from the enum's own XML docs so they cannot drift.</remarks>
internal static class EnumDocumentation
{
    private static readonly ConcurrentDictionary<Assembly, XDocument?> Documents = new();

    /// <summary>The description for an enum, keeping anything already written about the type.</summary>
    /// <param name="enumType">The enum being described.</param>
    /// <param name="existing">A description already generated for the schema, or null.</param>
    /// <returns>The existing text, if any, followed by one line per declared value.</returns>
    public static string Describe(Type enumType, string? existing = null)
    {
        var description = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(existing))
        {
            description.AppendLine(existing.Trim()).AppendLine();
        }

        foreach (var name in Enum.GetNames(enumType))
        {
            var number = Convert.ToInt32(Enum.Parse(enumType, name), CultureInfo.InvariantCulture);
            var meaning = Summary(enumType, name);

            description.AppendLine(meaning is null
                ? FormattableString.Invariant($"{number} = {name}")
                : FormattableString.Invariant($"{number} = {name} - {meaning}"));
        }

        return description.ToString().TrimEnd();
    }

    /// <summary>The <c>summary</c> the enum member carries in its XML documentation.</summary>
    private static string? Summary(Type enumType, string name)
    {
        var documentation = Documents.GetOrAdd(enumType.Assembly, Load);

        // An enum member is a field, so the XML names it "F:Namespace.Type.Member".
        var member = FormattableString.Invariant($"F:{enumType.FullName}.{name}");

        var summary = documentation?.Descendants("member")
            .FirstOrDefault(candidate => candidate.Attribute("name")?.Value == member)?
            .Element("summary")?.Value;

        return string.IsNullOrWhiteSpace(summary) ? null : Collapse(summary);
    }

    private static string Collapse(string summary) =>
        string.Join(' ', summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // Absent documentation degrades to bare numbers and names rather than failing the document.
    private static XDocument? Load(Assembly assembly)
    {
        var path = Path.ChangeExtension(assembly.Location, ".xml");

        return File.Exists(path) ? XDocument.Load(path) : null;
    }
}

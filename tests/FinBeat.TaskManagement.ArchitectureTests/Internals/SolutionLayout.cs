using System.Xml.Linq;

namespace FinBeat.TaskManagement.ArchitectureTests.Internals;

// Reads what the .csproj files declare. Worth doing separately from the IL because an unused
// reference leaves no trace in the compiled output - the compiler just drops it.
//
// Projects resolve by convention, <root>/src/<name>/<name>.csproj. Searching the tree instead breaks
// as soon as a second checkout sits inside the repo, which is what a worktree under the root is:
// every project name matches twice.
internal static class SolutionLayout
{
    private static readonly Lazy<DirectoryInfo> LazyRoot = new(FindRepositoryRoot);

    private static readonly string[] ProjectAreas = ["src", "tests"];

    private static DirectoryInfo RepositoryRoot => LazyRoot.Value;

    internal static FileInfo ProjectFile(string projectName)
    {
        foreach (var area in ProjectAreas)
        {
            var candidate = new FileInfo(
                Path.Combine(RepositoryRoot.FullName, area, projectName, projectName + ".csproj"));

            if (candidate.Exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"No project file for '{projectName}' under '{RepositoryRoot.FullName}'. Projects are "
            + $"expected at {string.Join(" or ", ProjectAreas.Select(area => $"{area}/<name>/<name>.csproj"))}.");
    }

    internal static IReadOnlyList<string> ProjectReferences(string projectName) =>
        ItemIncludes(projectName, "ProjectReference")
            // Solution and project files use Windows separators on any host OS.
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    internal static IReadOnlyList<string> PackageReferences(string projectName) =>
        SortedIncludes(projectName, "PackageReference");

    // Checked alongside packages: one FrameworkReference line pulls all of ASP.NET Core into a layer
    // without a single PackageReference.
    internal static IReadOnlyList<string> FrameworkReferences(string projectName) =>
        SortedIncludes(projectName, "FrameworkReference");

    private static string[] SortedIncludes(string projectName, string itemName) =>
        ItemIncludes(projectName, itemName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> ItemIncludes(string projectName, string itemName) =>
        XDocument.Load(ProjectFile(projectName).FullName)
            .Descendants()
            // By local name: SDK-style projects carry no XML namespace, legacy ones do.
            .Where(element => string.Equals(element.Name.LocalName, itemName, StringComparison.Ordinal))
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());

    private static DirectoryInfo FindRepositoryRoot()
    {
        // Upward, so running from inside a worktree finds that checkout's solution, not the parent's.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.sln").Any())
            {
                return directory;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate a solution file by walking up from '{AppContext.BaseDirectory}'. "
            + "The architecture rules resolve project paths relative to it.");
    }
}

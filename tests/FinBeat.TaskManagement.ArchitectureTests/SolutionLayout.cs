using System.Xml.Linq;

namespace FinBeat.TaskManagement.ArchitectureTests;

// Locates project files and reads what they declare, so the architecture rules can be asserted
// against the dependency graph the build *declares*. A reference added in the IDE but not yet used
// by any code leaves no trace in the compiled output - the compiler omits it - so reading the
// .csproj is the only angle that sees it while the layers are still thin.
//
// Projects resolve by convention, <root>/src/<name>/<name>.csproj, rather than by searching the
// tree. Searching is what breaks when a second checkout of the repository sits inside it, which is
// what a git worktree under the repository root produces: every project name then matches twice.
// Resolving by name never looks at anything it was not asked for, so that whole class of problem
// does not arise, and a project that moves out of the layout fails here loudly instead.
internal static class SolutionLayout
{
    private static readonly Lazy<DirectoryInfo> LazyRoot = new(FindRepositoryRoot);

    /// <summary>Directories that hold projects, in the order they are tried.</summary>
    private static readonly string[] ProjectAreas = ["src", "tests"];

    /// <summary>The directory holding the solution file.</summary>
    private static DirectoryInfo RepositoryRoot => LazyRoot.Value;

    /// <summary>The project file for <paramref name="projectName"/>.</summary>
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

    /// <summary>
    /// The project names <paramref name="projectName"/> declares a ProjectReference to, sorted and
    /// de-duplicated.
    /// </summary>
    internal static IReadOnlyList<string> ProjectReferences(string projectName) =>
        ItemIncludes(projectName, "ProjectReference")
            // .csproj paths use Windows separators whatever the host OS.
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The package ids <paramref name="projectName"/> declares a PackageReference to.</summary>
    internal static IReadOnlyList<string> PackageReferences(string projectName) =>
        SortedIncludes(projectName, "PackageReference");

    /// <summary>
    /// The shared frameworks <paramref name="projectName"/> declares a FrameworkReference to.
    /// </summary>
    /// <remarks>
    /// Checked alongside packages because a FrameworkReference is the other way third-party surface
    /// arrives without a PackageReference: one line pulls all of ASP.NET Core into a layer.
    /// </remarks>
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
            // Match on local name: SDK-style projects carry no XML namespace, legacy ones do.
            .Where(element => string.Equals(element.Name.LocalName, itemName, StringComparison.Ordinal))
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());

    private static DirectoryInfo FindRepositoryRoot()
    {
        // Walking *up* from the test assembly is what makes the suite resolve to the enclosing
        // checkout's own solution when it runs from inside a worktree.
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

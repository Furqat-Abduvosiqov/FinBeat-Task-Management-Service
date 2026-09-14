using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Reads the solution's project files straight off disk, so architecture rules can be asserted
/// against the dependency graph the build <em>declares</em>.
/// </summary>
/// <remarks>
/// <para>
/// This is the counterpart to inspecting compiled assemblies, and it catches a different class of
/// mistake. A reference added in the IDE but not yet used by any code leaves no trace in the
/// compiled output — the compiler simply omits it. Reading the .csproj sees it immediately, which
/// matters most while the layers are still thin.
/// </para>
/// <para>
/// The project list comes from the solution file rather than from globbing the directory tree.
/// That is the semantically correct source — the solution is what defines which projects are part
/// of it — and it is also the only version that survives a second checkout of the same repository
/// nested inside the working tree, which is exactly what a git worktree under the repository root
/// produces. Globbing finds both copies and every project name collides.
/// </para>
/// </remarks>
internal static partial class SolutionLayout
{
    private static readonly Lazy<FileInfo> LazySolutionFile = new(FindSolutionFile);
    private static readonly Lazy<IReadOnlyDictionary<string, FileInfo>> LazyProjectFiles = new(ReadProjectsFromSolution);

    /// <summary>The solution file that defines this repository's projects.</summary>
    internal static FileInfo SolutionFile => LazySolutionFile.Value;

    /// <summary>The directory holding the solution file.</summary>
    internal static DirectoryInfo RepositoryRoot => SolutionFile.Directory!;

    /// <summary>The project file for <paramref name="projectName"/>, as listed in the solution.</summary>
    internal static FileInfo ProjectFile(string projectName) =>
        LazyProjectFiles.Value.TryGetValue(projectName, out var file)
            ? file
            : throw new InvalidOperationException(
                $"'{projectName}' is not listed in '{SolutionFile.Name}'. "
                + $"Projects in the solution: {string.Join(", ", LazyProjectFiles.Value.Keys.OrderBy(name => name, StringComparer.Ordinal))}.");

    /// <summary>
    /// The project names <paramref name="projectName"/> declares a <c>&lt;ProjectReference&gt;</c> to,
    /// sorted and de-duplicated.
    /// </summary>
    internal static IReadOnlyList<string> ProjectReferences(string projectName) =>
        ItemIncludes(projectName, "ProjectReference")
            .Select(include => Path.GetFileNameWithoutExtension(NormaliseSeparators(include)))
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The package ids <paramref name="projectName"/> declares a <c>&lt;PackageReference&gt;</c> to.</summary>
    internal static IReadOnlyList<string> PackageReferences(string projectName) =>
        ItemIncludes(projectName, "PackageReference")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> ItemIncludes(string projectName, string itemName)
    {
        var document = XDocument.Load(ProjectFile(projectName).FullName);

        return document
            .Descendants()
            // Match on local name: SDK-style projects carry no XML namespace, legacy ones do.
            .Where(element => string.Equals(element.Name.LocalName, itemName, StringComparison.Ordinal))
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
    }

    private static FileInfo FindSolutionFile()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var solution = directory.EnumerateFiles("*.sln").Concat(directory.EnumerateFiles("*.slnx")).FirstOrDefault();

            if (solution is not null)
            {
                return solution;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate a solution file by walking up from '{AppContext.BaseDirectory}'. "
            + "The architecture rules read the project list from it and cannot run without it.");
    }

    private static Dictionary<string, FileInfo> ReadProjectsFromSolution()
    {
        var solution = SolutionFile;
        var root = solution.Directory!.FullName;
        var projects = new Dictionary<string, FileInfo>(StringComparer.Ordinal);

        foreach (var relativePath in ProjectPathsIn(solution))
        {
            // Solution files record Windows separators whatever the host OS.
            var fullPath = Path.GetFullPath(Path.Combine(root, NormaliseSeparators(relativePath)));
            var name = Path.GetFileNameWithoutExtension(fullPath);

            if (projects.TryGetValue(name, out var existing))
            {
                throw new InvalidOperationException(
                    $"'{solution.Name}' lists two projects named '{name}':{Environment.NewLine}"
                    + $"  - {existing.FullName}{Environment.NewLine}"
                    + $"  - {fullPath}{Environment.NewLine}"
                    + "The architecture rules address projects by name, so the names have to be unique.");
            }

            projects.Add(name, new FileInfo(fullPath));
        }

        return projects;
    }

    private static IEnumerable<string> ProjectPathsIn(FileInfo solution)
    {
        if (string.Equals(solution.Extension, ".slnx", StringComparison.OrdinalIgnoreCase))
        {
            return XDocument.Load(solution.FullName)
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Project", StringComparison.Ordinal))
                .Select(element => element.Attribute("Path")?.Value)
                .Where(path => IsCSharpProject(path))
                .Select(path => path!);
        }

        // Classic .sln: Project("{type guid}") = "Display name", "relative\path.csproj", "{project guid}"
        return SolutionProjectLine()
            .Matches(File.ReadAllText(solution.FullName))
            .Select(match => match.Groups["path"].Value)
            // Solution folders are recorded the same way but carry no project file extension.
            .Where(path => IsCSharpProject(path));
    }

    private static bool IsCSharpProject(string? path) =>
        path is not null && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);

    private static string NormaliseSeparators(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    // The closing quote of the path needs no match of its own: [^"]+ already stops at it.
    [GeneratedRegex("""^Project\("\{[^}]*\}"\)\s*=\s*"[^"]*"\s*,\s*"(?<path>[^"]+)""", RegexOptions.Multiline)]
    private static partial Regex SolutionProjectLine();
}

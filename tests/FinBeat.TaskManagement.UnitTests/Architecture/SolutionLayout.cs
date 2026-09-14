using System.Xml.Linq;

namespace FinBeat.TaskManagement.UnitTests.Architecture;

/// <summary>
/// Reads a project's <c>.csproj</c> file straight off disk, so architecture rules can be asserted
/// against the dependency graph the build <em>declares</em>.
/// </summary>
/// <remarks>
/// This is the counterpart to inspecting a compiled assembly, and it catches a different class of
/// mistake: a <c>&lt;PackageReference&gt;</c> or <c>&lt;ProjectReference&gt;</c> added in the IDE
/// but never actually used by any code leaves no trace in the compiled output, because the compiler
/// simply omits the unused reference — but it is still a declared dependency the project would pull
/// in, and it fails a restore-time or IDE-time check immediately, long before anyone writes code
/// that would make a compiled-assembly check notice it too.
/// </remarks>
internal static class SolutionLayout
{
    private static readonly Lazy<DirectoryInfo> LazyRoot = new(FindRepositoryRoot);
    private static readonly Lazy<IReadOnlyDictionary<string, FileInfo>> LazyProjectFiles = new(DiscoverProjectFiles);

    /// <summary>The directory holding the solution file.</summary>
    internal static DirectoryInfo RepositoryRoot => LazyRoot.Value;

    /// <summary>The project file for <paramref name="projectName"/>, located by file name.</summary>
    internal static FileInfo ProjectFile(string projectName) =>
        LazyProjectFiles.Value.TryGetValue(projectName, out var file)
            ? file
            : throw new InvalidOperationException(
                $"No project file named '{projectName}.csproj' was found under '{RepositoryRoot.FullName}'. "
                + $"Projects found: {string.Join(", ", LazyProjectFiles.Value.Keys.OrderBy(name => name, StringComparer.Ordinal))}.");

    /// <summary>
    /// The project names <paramref name="projectName"/> declares a <c>&lt;ProjectReference&gt;</c> to,
    /// sorted and de-duplicated.
    /// </summary>
    internal static IReadOnlyList<string> ProjectReferences(string projectName) =>
        ItemIncludes(projectName, "ProjectReference")
            // .csproj paths use Windows separators regardless of host OS; normalise before taking the file name.
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
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

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.sln").Any() || directory.EnumerateFiles("*.slnx").Any())
            {
                return directory;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate the solution root by walking up from '{AppContext.BaseDirectory}'. "
            + "The architecture rules read .csproj files from disk and cannot run without it.");
    }

    private static Dictionary<string, FileInfo> DiscoverProjectFiles()
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        return RepositoryRoot
            .EnumerateFiles("*.csproj", options)
            .Where(IsNotBuildOutput)
            .ToDictionary(file => Path.GetFileNameWithoutExtension(file.Name), file => file, StringComparer.Ordinal);
    }

    private static bool IsNotBuildOutput(FileInfo file) =>
        !Path.GetRelativePath(RepositoryRoot.FullName, file.DirectoryName!)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj" or ".git");
}

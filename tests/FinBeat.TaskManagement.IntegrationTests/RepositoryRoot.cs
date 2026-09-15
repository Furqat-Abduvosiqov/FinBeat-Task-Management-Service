namespace FinBeat.TaskManagement.IntegrationTests;

/// <summary>Finds the repository root from the test binaries, for tests that read files it ships.</summary>
internal static class RepositoryRoot
{
    /// <summary>The directory holding docker-compose.yml, walking up from the test output folder.</summary>
    /// <exception cref="InvalidOperationException">No such directory exists above the binaries.</exception>
    public static DirectoryInfo Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("No docker-compose.yml above the test binaries.");
    }

    /// <summary>A path to a file the repository ships.</summary>
    /// <param name="segments">Path segments below the root.</param>
    public static string Combine(params string[] segments) => Path.Combine([Find().FullName, .. segments]);
}

using System.Text.RegularExpressions;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests;

/// <summary>Holds the repository to one version of each container image.</summary>
/// <remarks>
/// Two tags of the same image is a second download of a dependency that was already there, and -
/// worse - a test exercising a different build than the stack runs. This reads the images out of
/// docker-compose.yml, the Dockerfiles and <see cref="TestImages"/>, and fails when they disagree.
/// </remarks>
public sealed class ContainerImageTests
{
    [Fact]
    public void Every_image_is_declared_at_one_version_across_the_repository()
    {
        var references = AllImageReferences();

        // An empty set would pass the grouping below without comparing anything.
        references.ShouldNotBeEmpty();

        var conflicts = references
            .GroupBy(image => image.Repository, StringComparer.Ordinal)
            .Where(group => group.Select(image => image.Tag).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(image => $"{image.Tag} ({image.Source})"))}")
            .ToArray();

        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public void Every_image_a_test_starts_is_one_the_compose_stack_runs()
    {
        // The stricter half of the rule above: matching versions is not enough if a test reaches for
        // a different variant of the same image, which is its own second download.
        var compose = AllImageReferences()
            .Where(image => image.Source == "docker-compose.yml")
            .Select(image => image.Reference)
            .ToArray();

        compose.ShouldNotBeEmpty();

        foreach (var image in TestImages.All)
        {
            compose.ShouldContain(image, $"docker-compose.yml runs {string.Join(", ", compose)}");
        }
    }

    private static ImageReference[] AllImageReferences()
    {
        var root = RepositoryRoot();

        var compose = Regex
            .Matches(File.ReadAllText(Path.Combine(root.FullName, "docker-compose.yml")), @"^\s*image:\s*(\S+)", RegexOptions.Multiline)
            .Select(match => ImageReference.Parse(match.Groups[1].Value, "docker-compose.yml"));

        var dockerfiles = root
            .EnumerateFiles("*Dockerfile", SearchOption.AllDirectories)
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => Regex
                .Matches(File.ReadAllText(file.FullName), @"^FROM\s+(\S+)", RegexOptions.Multiline)
                .Select(match => ImageReference.Parse(match.Groups[1].Value, file.Name)));

        var tests = TestImages.All.Select(image => ImageReference.Parse(image, nameof(TestImages)));

        return [.. compose, .. dockerfiles, .. tests];
    }

    /// <summary>Walks up from the test binaries to the directory holding docker-compose.yml.</summary>
    private static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("No docker-compose.yml above the test binaries.");
    }

    private sealed record ImageReference(string Repository, string Tag, string Source)
    {
        public string Reference => $"{Repository}:{Tag}";

        public static ImageReference Parse(string reference, string source)
        {
            // Split on the last colon: the repository may carry a registry host and a path, and a
            // digest-pinned reference would have an @ that never reaches here.
            var separator = reference.LastIndexOf(':');

            return separator < 0
                ? new ImageReference(reference, "latest", source)
                : new ImageReference(reference[..separator], reference[(separator + 1)..], source);
        }
    }
}

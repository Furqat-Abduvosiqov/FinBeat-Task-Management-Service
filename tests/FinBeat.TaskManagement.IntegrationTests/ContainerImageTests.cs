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

    [Fact]
    public void No_fixture_names_a_container_image_in_a_string_literal()
    {
        // AllImageReferences compares TestImages against the repository; nothing compares TestImages
        // against the fixtures that actually start containers. A literal at a WithImage call site is
        // drift the other two facts here are structurally unable to see.
        var sources = new DirectoryInfo(Path.Combine(RepositoryRoot.Find().FullName, "tests"))
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => file.Name != $"{nameof(ContainerImageTests)}.cs")
            .ToArray();

        // An empty scan would pass the assertion below without reading anything.
        sources.ShouldNotBeEmpty();

        sources
            .Where(file => Regex.IsMatch(File.ReadAllText(file.FullName), @"WithImage\(\s*"""))
            .Select(file => file.Name)
            .ShouldBeEmpty("WithImage takes a TestImages member, never a literal.");
    }

    private static ImageReference[] AllImageReferences()
    {
        var root = RepositoryRoot.Find();

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

    private sealed record ImageReference(string Repository, string Tag, string Source)
    {
        public string Reference => $"{Repository}:{Tag}";

        public static ImageReference Parse(string reference, string source)
        {
            // repository[:tag][@algo:digest]. Strip the digest before splitting the tag off, or the
            // last colon lands inside the digest and the repository key silently becomes
            // "repo:tag@sha256" - which groups a pinned reference apart from an unpinned one and
            // stops the conflict check from seeing them as the same image.
            var digestAt = reference.IndexOf('@');
            var digest = digestAt < 0 ? string.Empty : reference[digestAt..];
            var name = digestAt < 0 ? reference : reference[..digestAt];

            var separator = name.LastIndexOf(':');

            return separator < 0
                ? new ImageReference(name, "latest" + digest, source)
                : new ImageReference(name[..separator], name[(separator + 1)..] + digest, source);
        }
    }
}

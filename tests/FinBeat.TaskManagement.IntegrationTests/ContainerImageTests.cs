using System.Text.RegularExpressions;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests;

/// <summary>Keeps the images the tests start byte-identical to the ones the compose stack runs.</summary>
public sealed class ContainerImageTests
{
    [Fact]
    public void Every_image_a_test_starts_is_one_the_compose_stack_runs()
    {
        // A tag match is not enough: a test could still reach for a different digest of the same
        // image, which is its own second download and its own drift from what production runs.
        var compose = ComposeImages();

        compose.ShouldNotBeEmpty();

        foreach (var image in TestImages.All)
        {
            compose.ShouldContain(image, $"docker-compose.yml runs {string.Join(", ", compose)}");
        }
    }

    [Fact]
    public void No_fixture_names_a_container_image_in_a_string_literal()
    {
        // The fact above only compares TestImages against docker-compose.yml; nothing compares
        // TestImages against the fixtures that actually start containers. A literal at a WithImage
        // call site is drift that fact is structurally unable to see.
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

    private static string[] ComposeImages()
    {
        var root = RepositoryRoot.Find();

        return [.. Regex
            .Matches(File.ReadAllText(Path.Combine(root.FullName, "docker-compose.yml")), @"^\s*image:\s*(\S+)", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)];
    }
}

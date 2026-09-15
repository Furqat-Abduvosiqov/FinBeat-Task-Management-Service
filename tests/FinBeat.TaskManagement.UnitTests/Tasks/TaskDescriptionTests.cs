using FinBeat.TaskManagement.Domain.Tasks.Exceptions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskDescriptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_null_or_whitespace_produces_none(string? value)
    {
        var description = TaskDescription.Create(value);

        description.ShouldBe(TaskDescription.None);
        description.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Create_trims_surrounding_whitespace()
    {
        var description = TaskDescription.Create("  Remember the eggs  ");

        description.Value.ShouldBe("Remember the eggs");
    }

    [Fact]
    public void Create_accepts_a_description_of_exactly_max_length()
    {
        var value = new string('a', TaskDescription.MaxLength);

        var description = TaskDescription.Create(value);

        description.Value.Length.ShouldBe(TaskDescription.MaxLength);
    }

    [Fact]
    public void Create_rejects_a_description_longer_than_max_length()
    {
        var value = new string('a', TaskDescription.MaxLength + 1);

        Should.Throw<InvalidTaskDescriptionException>(() => TaskDescription.Create(value));
    }
}

using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.Exceptions;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskTitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_null_empty_or_whitespace_throws(string? value)
    {
        Should.Throw<InvalidTaskTitleException>(() => TaskTitle.Create(value));
    }

    [Fact]
    public void Create_trims_surrounding_whitespace()
    {
        var title = TaskTitle.Create("  Buy milk  ");

        title.Value.ShouldBe("Buy milk");
    }

    [Fact]
    public void Create_accepts_a_title_of_exactly_max_length()
    {
        var value = new string('a', TaskTitle.MaxLength);

        var title = TaskTitle.Create(value);

        title.Value.Length.ShouldBe(TaskTitle.MaxLength);
    }

    [Fact]
    public void Create_rejects_a_title_longer_than_max_length()
    {
        var value = new string('a', TaskTitle.MaxLength + 1);

        Should.Throw<InvalidTaskTitleException>(() => TaskTitle.Create(value));
    }

    [Fact]
    public void Empty_and_too_long_failures_carry_distinct_messages()
    {
        var emptyFailure = Should.Throw<InvalidTaskTitleException>(() => TaskTitle.Create(" "));
        var tooLongFailure = Should.Throw<InvalidTaskTitleException>(() => TaskTitle.Create(new string('a', TaskTitle.MaxLength + 1)));

        emptyFailure.Message.ShouldNotBe(tooLongFailure.Message);
    }

    [Fact]
    public void Titles_with_the_same_text_are_equal()
    {
        var first = TaskTitle.Create("Buy milk");
        var second = TaskTitle.Create("Buy milk");

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }
}

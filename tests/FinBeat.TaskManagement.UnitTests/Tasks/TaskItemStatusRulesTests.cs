using System.Reflection;
using FinBeat.TaskManagement.Domain.Tasks;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskItemStatusRulesTests
{
    [Theory]
    [InlineData(TaskItemStatus.New, TaskItemStatus.New, false)]
    [InlineData(TaskItemStatus.New, TaskItemStatus.InProgress, true)]
    [InlineData(TaskItemStatus.New, TaskItemStatus.Completed, true)]
    [InlineData(TaskItemStatus.New, TaskItemStatus.Archived, true)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.New, true)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.InProgress, false)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Completed, true)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Archived, true)]
    [InlineData(TaskItemStatus.Completed, TaskItemStatus.New, false)]
    [InlineData(TaskItemStatus.Completed, TaskItemStatus.InProgress, true)]
    [InlineData(TaskItemStatus.Completed, TaskItemStatus.Completed, false)]
    [InlineData(TaskItemStatus.Completed, TaskItemStatus.Archived, true)]
    [InlineData(TaskItemStatus.Archived, TaskItemStatus.New, true)]
    [InlineData(TaskItemStatus.Archived, TaskItemStatus.InProgress, false)]
    [InlineData(TaskItemStatus.Archived, TaskItemStatus.Completed, false)]
    [InlineData(TaskItemStatus.Archived, TaskItemStatus.Archived, false)]
    public void CanTransition_matches_the_documented_matrix(TaskItemStatus from, TaskItemStatus to, bool expected)
    {
        TaskItemStatusRules.CanTransition(from, to).ShouldBe(expected);
    }

    // Guards against a fifth TaskItemStatus being added without extending the matrix above.
    //
    // Asserting that CanTransition "does not throw" for every enum value would NOT catch that:
    // CanTransition ends in a blanket `_ => false` discard arm, which is the correct design but
    // also means the method can never throw for any value, present or future. A new status would
    // silently return false for every pair and such a test would stay green. Asserting that the
    // theory data itself covers the full cross product is what actually fails.
    [Fact]
    public void Theory_data_covers_every_ordered_pair_of_declared_statuses()
    {
        var statuses = Enum.GetValues<TaskItemStatus>();
        var method = typeof(TaskItemStatusRulesTests)
            .GetMethod(nameof(CanTransition_matches_the_documented_matrix))!;

        var covered = method
            .GetCustomAttributes<InlineDataAttribute>()
            .SelectMany(attribute => attribute.GetData(method))
            .Select(row => ((TaskItemStatus)row[0]!, (TaskItemStatus)row[1]!))
            .ToHashSet();

        var expected = statuses
            .SelectMany(_ => statuses, (from, to) => (from, to))
            .ToHashSet();

        covered.Count.ShouldBe(
            statuses.Length * statuses.Length,
            $"the matrix test must cover all {statuses.Length * statuses.Length} ordered pairs of "
            + $"the {statuses.Length} declared statuses");

        expected.Except(covered).ShouldBeEmpty("these status pairs are missing from the theory data");
    }
}

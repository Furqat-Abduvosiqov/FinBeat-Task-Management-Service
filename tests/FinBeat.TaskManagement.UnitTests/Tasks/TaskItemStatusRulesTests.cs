using System.Reflection;
using FinBeat.TaskManagement.Domain.Tasks;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public sealed class TaskItemStatusRulesTests
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

    // Catches a fifth status being added without extending the matrix. Checking that CanTransition
    // "does not throw" would not: its `_ => false` arm means it never can, so such a test is green
    // by construction. Checking the theory data's own coverage is what actually fails.
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

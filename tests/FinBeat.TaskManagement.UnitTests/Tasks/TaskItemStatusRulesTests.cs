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

    [Fact]
    public void Every_declared_status_is_handled_by_CanTransition_without_throwing()
    {
        var statuses = Enum.GetValues<TaskItemStatus>();

        foreach (var from in statuses)
        {
            foreach (var to in statuses)
            {
                Should.NotThrow(() => TaskItemStatusRules.CanTransition(from, to));
            }
        }
    }
}

using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskItemIdTests
{
    [Fact]
    public void From_with_empty_guid_throws()
    {
        Should.Throw<ArgumentException>(() => TaskItemId.From(Guid.Empty));
    }

    [Fact]
    public void From_with_a_non_empty_guid_wraps_it()
    {
        var value = Guid.NewGuid();

        var id = TaskItemId.From(value);

        id.Value.ShouldBe(value);
    }

    [Fact]
    public void New_assigns_a_non_empty_value()
    {
        TaskItemId.New().Value.ShouldNotBe(Guid.Empty);
    }

    // The property the aggregate actually depends on: an id is unique from the moment it is minted,
    // so two freshly created tasks are never mistaken for one another before either is saved.
    [Fact]
    public void New_produces_a_distinct_id_every_time()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => TaskItemId.New()).ToArray();

        ids.Distinct().Count().ShouldBe(ids.Length);
    }

    [Fact]
    public void Ids_wrapping_the_same_guid_are_equal()
    {
        var value = Guid.NewGuid();

        TaskItemId.From(value).ShouldBe(TaskItemId.From(value));
    }
}

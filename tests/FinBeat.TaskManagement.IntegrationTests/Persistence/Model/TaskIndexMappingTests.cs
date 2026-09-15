using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskIndexMappingTests(ModelFixture fixture)
{
    [Fact]
    public void Exactly_one_non_unique_index_covers_Status_then_CreatedAt_in_that_order()
    {
        var index = fixture.TaskEntityType.GetIndexes().ShouldHaveSingleItem();

        // Order matters here - it is one composite index, not two single-column ones - so this is a
        // plain, order-sensitive ShouldBe.
        index.Properties.Select(property => property.Name)
            .ShouldBe([nameof(TaskItem.Status), nameof(TaskItem.CreatedAt)]);
        index.GetDatabaseName().ShouldBe("ix_tasks_status_created_at");
        index.IsUnique.ShouldBeFalse();
    }
}

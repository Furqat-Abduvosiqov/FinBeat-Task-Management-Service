using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskIndexMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskIndexMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void Exactly_one_non_unique_index_covers_Status_then_CreatedAt_in_that_order()
    {
        var index = _fixture.TaskEntityType.GetIndexes().ShouldHaveSingleItem();

        // Order matters here - it is one composite index, not two single-column ones - so this is a
        // plain, order-sensitive ShouldBe.
        index.Properties.Select(property => property.Name)
            .ShouldBe([nameof(TaskItem.Status), nameof(TaskItem.CreatedAt)]);
        index.GetDatabaseName().ShouldBe("ix_tasks_status_created_at");
        index.IsUnique.ShouldBeFalse();
    }
}

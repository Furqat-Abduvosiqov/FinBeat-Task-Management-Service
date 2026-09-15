using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskTableAndColumnMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskTableAndColumnMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void TaskItem_maps_to_the_tasks_table_with_no_schema()
    {
        _fixture.TaskEntityType.GetTableName().ShouldBe("tasks");
        _fixture.TaskEntityType.GetSchema().ShouldBeNull();
    }

    [Fact]
    public void TaskItem_maps_exactly_the_expected_columns_and_nothing_else()
    {
        // A shadow property, a forgotten Ignore(DomainEvents), or a property added without a
        // migration all change this set - in either direction.
        var columnNames = _fixture.TaskEntityType.GetProperties()
            .Select(property => property.GetColumnName(_fixture.TaskTable))
            .ToArray();

        columnNames.ShouldBe(
            ["id", "title", "description", "status", "created_at", "updated_at"],
            ignoreOrder: true);
    }
}

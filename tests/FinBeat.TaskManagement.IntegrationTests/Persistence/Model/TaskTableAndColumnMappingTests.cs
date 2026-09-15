using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskTableAndColumnMappingTests(ModelFixture fixture)
{
    [Fact]
    public void TaskItem_maps_to_the_tasks_table_with_no_schema()
    {
        fixture.TaskEntityType.GetTableName().ShouldBe("tasks");
        fixture.TaskEntityType.GetSchema().ShouldBeNull();
    }

    [Fact]
    public void TaskItem_maps_exactly_the_expected_columns_and_nothing_else()
    {
        // A shadow property, a forgotten Ignore(DomainEvents), or a property added without a
        // migration all change this set - in either direction.
        var columnNames = fixture.TaskEntityType.GetProperties()
            .Select(property => property.GetColumnName(fixture.TaskTable))
            .ToArray();

        columnNames.ShouldBe(
            ["id", "title", "description", "status", "created_at", "updated_at"],
            ignoreOrder: true);
    }
}

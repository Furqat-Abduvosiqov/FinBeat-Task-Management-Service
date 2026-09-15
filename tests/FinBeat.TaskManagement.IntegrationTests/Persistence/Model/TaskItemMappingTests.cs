using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskItemMappingTests(ModelFixture fixture)
{
    [Fact]
    public void TaskItem_maps_to_the_tasks_table_with_exactly_the_expected_columns()
    {
        fixture.TaskEntityType.GetTableName().ShouldBe("tasks");
        fixture.TaskEntityType.GetSchema().ShouldBeNull();

        // A shadow property or an unmapped addition would change this set, in either direction.
        var columnNames = fixture.TaskEntityType.GetProperties()
            .Select(property => property.GetColumnName(fixture.TaskTable))
            .ToArray();

        columnNames.ShouldBe(
            ["id", "title", "description", "status", "created_at", "updated_at"],
            ignoreOrder: true);
    }

    [Fact]
    public void Id_is_the_primary_key_and_is_assigned_by_the_domain()
    {
        var primaryKey = fixture.TaskEntityType.FindPrimaryKey().ShouldNotBeNull();
        primaryKey.Properties.ShouldHaveSingleItem().Name.ShouldBe(nameof(TaskItem.Id));

        var property = fixture.GetRequiredProperty(nameof(TaskItem.Id));

        // ValueGeneratedOnAdd would have PostgreSQL generate the id, overwriting the one
        // TaskItemId.New() already assigned.
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.GetColumnType().ShouldBe("uuid");
    }

    [Fact]
    public void Title_and_description_are_bounded_by_the_lengths_the_domain_declares()
    {
        var title = fixture.GetRequiredProperty(nameof(TaskItem.Title));

        title.IsNullable.ShouldBeFalse();
        title.GetColumnType().ShouldBe($"character varying({TaskTitle.MaxLength})");

        // TaskDescription.None is an empty string, not null; a nullable column would load back as null.
        var description = fixture.GetRequiredProperty(nameof(TaskItem.Description));

        description.IsNullable.ShouldBeFalse();
        description.GetColumnType().ShouldBe($"character varying({TaskDescription.MaxLength})");
    }

    [Fact]
    public void Status_is_stored_as_the_int_the_enum_declares()
    {
        var property = fixture.GetRequiredProperty(nameof(TaskItem.Status));

        property.GetColumnType().ShouldBe("integer");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Exactly_one_non_unique_index_covers_Status_then_CreatedAt_in_that_order()
    {
        var index = fixture.TaskEntityType.GetIndexes().ShouldHaveSingleItem();

        // Order matters: one composite index, not two single-column ones.
        index.Properties.Select(property => property.Name)
            .ShouldBe([nameof(TaskItem.Status), nameof(TaskItem.CreatedAt)]);
        index.GetDatabaseName().ShouldBe("ix_tasks_status_created_at");
        index.IsUnique.ShouldBeFalse();
    }
}

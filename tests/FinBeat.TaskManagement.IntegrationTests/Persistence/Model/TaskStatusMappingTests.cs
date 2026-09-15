using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskStatusMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskStatusMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void Status_column_is_the_native_postgres_enum_with_no_value_converter()
    {
        var property = _fixture.TaskEntityType.FindProperty(nameof(TaskItem.Status)).ShouldNotBeNull();

        property.GetColumnType().ShouldBe(PostgresEnumMapping.TaskItemStatusTypeName);
        // A value converter here would mean HasPostgresEnum was dropped and the column fell back to
        // an integer, or .HasConversion<int>() was added on top of the enum column.
        property.GetValueConverter().ShouldBeNull();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Exactly_one_postgres_enum_is_registered_with_the_expected_labels_in_declaration_order()
    {
        IReadOnlyList<PostgresEnum> enums = _fixture.DesignTimeModel.GetPostgresEnums();
        var enumType = enums.ShouldHaveSingleItem();

        enumType.Name.ShouldBe(PostgresEnumMapping.TaskItemStatusTypeName);
        enumType.Schema.ShouldBeNull();

        // Hard-coded, deliberately: this is what has to change by hand, alongside a migration, the
        // day TaskItemStatus grows, is reordered, or is renamed. Deriving the expectation from
        // Enum.GetNames would make the test agree with whatever the enum says, including a wrong change.
        enumType.Labels.ShouldBe(["new", "in_progress", "completed", "archived"]);
    }
}

using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskPrimaryKeyMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskPrimaryKeyMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void Primary_key_is_Id_alone()
    {
        var primaryKey = _fixture.TaskEntityType.FindPrimaryKey().ShouldNotBeNull();
        var keyProperty = primaryKey.Properties.ShouldHaveSingleItem();

        keyProperty.Name.ShouldBe(nameof(TaskItem.Id));
    }

    [Fact]
    public void Id_is_assigned_by_the_domain_not_generated_by_the_database()
    {
        var property = _fixture.TaskEntityType.FindProperty(nameof(TaskItem.Id)).ShouldNotBeNull();

        // TaskItemId.New() assigns the id in the domain, before the row exists. ValueGeneratedOnAdd
        // would tell EF to expect PostgreSQL to produce it instead, overwriting the id already set.
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.GetColumnType().ShouldBe("uuid");
        property.IsNullable.ShouldBeFalse();
    }
}

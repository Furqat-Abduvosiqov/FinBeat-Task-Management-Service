using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskPrimaryKeyMappingTests(ModelFixture fixture)
{
    [Fact]
    public void Primary_key_is_Id_alone()
    {
        var primaryKey = fixture.TaskEntityType.FindPrimaryKey().ShouldNotBeNull();
        var keyProperty = primaryKey.Properties.ShouldHaveSingleItem();

        keyProperty.Name.ShouldBe(nameof(TaskItem.Id));
    }

    [Fact]
    public void Id_is_assigned_by_the_domain_not_generated_by_the_database()
    {
        var property = fixture.GetRequiredProperty(nameof(TaskItem.Id));

        // TaskItemId.New() assigns the id in the domain, before the row exists. ValueGeneratedOnAdd
        // would tell EF to expect PostgreSQL to produce it instead, overwriting the id already set.
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.GetColumnType().ShouldBe("uuid");
        property.IsNullable.ShouldBeFalse();

        // Exercised in both directions rather than asserted by runtime type name: HasConversion(lambda,
        // lambda) compiles to a compiler-generated ValueConverter<T,U> whose GetType().Name carries no
        // meaning.
        var converter = property.GetValueConverter().ShouldNotBeNull();
        converter.ProviderClrType.ShouldBe(typeof(Guid));
        var guid = Guid.NewGuid();
        converter.ConvertFromProvider(guid).ShouldBeOfType<TaskItemId>().Value.ShouldBe(guid);
        converter.ConvertToProvider(new TaskItemId(guid)).ShouldBe(guid);
    }
}

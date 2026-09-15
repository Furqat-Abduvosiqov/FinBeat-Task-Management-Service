using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskStatusMappingTests(ModelFixture fixture)
{
    [Fact]
    public void Status_is_stored_as_the_int_the_enum_declares()
    {
        var property = fixture.GetRequiredProperty(nameof(TaskItem.Status));

        // integer, and nothing else. A stray HasConversion<string>() would make it text, and a native
        // PostgreSQL enum type would make it something the database has to be taught about first.
        property.GetColumnType().ShouldBe("integer");
        property.ClrType.ShouldBe(typeof(TaskItemStatus));
        property.IsNullable.ShouldBeFalse();

        // Null on purpose: an int-backed enum maps to integer natively, so EF installs no converter.
        // If one ever appears here, something has been layered on top of the default mapping.
        property.GetValueConverter().ShouldBeNull();
    }

    [Fact]
    public void A_check_constraint_restricts_the_column_to_the_declared_statuses()
    {
        // The design-time model, not the runtime one: check constraints are stripped from the
        // read-optimised model EF serves at runtime, so asking TaskEntityType for them throws.
        var entityType = fixture.DesignTimeModel.FindEntityType(typeof(TaskItem)).ShouldNotBeNull();
        var checkConstraint = entityType.GetCheckConstraints().ShouldHaveSingleItem();

        checkConstraint.Name.ShouldBe("ck_tasks_status");

        // Hard-coded, deliberately. An int column accepts any int, so this constraint is the only thing
        // keeping a 0 - the value the enum pointedly has no member for - or a 5 out of the table. The
        // day TaskItemStatus grows, this expectation has to change by hand alongside a migration;
        // deriving it from Enum.GetValues would make the test agree with whatever the enum says,
        // including a wrong change.
        checkConstraint.Sql.ShouldBe("status IN (1, 2, 3, 4)");
    }
}

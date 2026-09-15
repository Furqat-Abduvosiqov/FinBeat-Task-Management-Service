using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class MigrationDriftTests
{
    [Fact]
    public void The_model_has_not_changed_since_the_last_migration()
    {
        // Every other test in this folder asserts what the model says. This one asserts that the
        // migrations agree with it - which is the failure the others cannot see. Change a max length,
        // add a property, rename a column, and the model tests can be updated to match and still pass
        // while the database is left describing the old shape.
        //
        // This is the same comparison `dotnet ef migrations has-pending-model-changes` performs. It
        // lives here as well because there is no CI in this repository to run the CLI form.
        using var context = ModelFixture.CreateDesignTimeContext();

        // Reached through IMigrationsAssembly rather than by naming ApplicationDbContextModelSnapshot:
        // EF scaffolds that class without an access modifier, so it is internal to Infrastructure.
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot;
        snapshot.ShouldNotBeNull("no model snapshot was found - has a migration ever been added?");

        // The snapshot's model is raw metadata; it has to be finalised before it is comparable to a
        // model EF built for a live context.
        var snapshotModel = context.GetService<IModelRuntimeInitializer>()
            .Initialize(snapshot.Model, designTime: true, validationLogger: null);

        var differences = context.GetService<IMigrationsModelDiffer>()
            .GetDifferences(snapshotModel.GetRelationalModel(), context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        // Named rather than counted, so a failure says which operation is outstanding.
        differences
            .Select(operation => operation.GetType().Name)
            .ShouldBeEmpty("the model has moved since the last migration - run dotnet ef migrations add");
    }
}

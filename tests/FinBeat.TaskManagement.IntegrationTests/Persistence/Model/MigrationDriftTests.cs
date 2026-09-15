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
        // The other tests in this folder assert what the model says; this one asserts the migrations
        // agree with it, mirroring `dotnet ef migrations has-pending-model-changes` since there is no
        // CI here to run the CLI form.
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

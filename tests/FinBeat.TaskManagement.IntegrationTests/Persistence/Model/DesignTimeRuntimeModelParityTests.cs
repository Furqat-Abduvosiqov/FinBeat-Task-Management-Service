using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class DesignTimeRuntimeModelParityTests(ModelFixture fixture)
{
    [Fact]
    public void Design_time_model_and_runtime_model_agree_on_shape()
    {
        // Two contexts built completely differently - one from DI through AddInfrastructure, one from a
        // bare connection string the way dotnet ef does - have to produce the same model. If they ever
        // stop, a migration gets scaffolded against one shape while the running application queries
        // another, which compiles cleanly and surfaces much later as a missing relation or column.
        //
        // This is not hypothetical: an earlier arrangement applied part of the provider configuration
        // at each call site rather than inside the context, and one path missed it, so the scaffolded
        // column type disagreed with the one the application expected. Configuration that every context
        // gets by construction - as OnConfiguring now does for snake_case naming - is what this test
        // keeps honest.
        using var designTimeContext = ModelFixture.CreateDesignTimeContext();

        // Both sides read the design-time model rather than context.Model, because EF strips
        // design-time-only configuration - check constraints among it - from the runtime model.
        // Comparing runtime models would leave those out of the comparison on both sides, and the
        // assertion would be weaker than it reads.
        var designTimeModel = designTimeContext.GetService<IDesignTimeModel>().Model;
        var runtimeModel = fixture.DesignTimeModel;

        // EF's own differ, rather than a hand-rolled projection of columns, indexes and enum labels: a
        // hand-rolled comparison only catches what it remembers to list, so a default value, a
        // collation, a check constraint or a future foreign key could diverge silently. This is the
        // same comparison MigrationDriftTests makes, pointed at two models instead of a model and a
        // snapshot.
        designTimeContext.GetService<IMigrationsModelDiffer>()
            .GetDifferences(designTimeModel.GetRelationalModel(), runtimeModel.GetRelationalModel())
            .Select(operation => operation.GetType().Name)
            .ShouldBeEmpty("the design-time model and the runtime model have diverged");
    }
}

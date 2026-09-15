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
        // A DI-built context and a bare-connection-string one (as dotnet ef builds) must agree, or a
        // migration gets scaffolded against a shape the running application does not actually query.
        using var designTimeContext = ModelFixture.CreateDesignTimeContext();

        // Design-time model on both sides: EF strips design-time-only configuration (check
        // constraints included) from the runtime model, which would weaken this comparison.
        var designTimeModel = designTimeContext.GetService<IDesignTimeModel>().Model;
        var runtimeModel = fixture.DesignTimeModel;

        // EF's own differ, not a hand-rolled comparison, so no default value, collation or
        // constraint can diverge silently without this test noticing.
        designTimeContext.GetService<IMigrationsModelDiffer>()
            .GetDifferences(designTimeModel.GetRelationalModel(), runtimeModel.GetRelationalModel())
            .Select(operation => operation.GetType().Name)
            .ShouldBeEmpty("the design-time model and the runtime model have diverged");
    }
}

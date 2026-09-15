using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class DesignTimeRuntimeModelParityTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public DesignTimeRuntimeModelParityTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void Design_time_model_and_runtime_model_agree_on_shape()
    {
        // If the design-time factory ever stopped routing through ApplicationDbContextOptions - say,
        // a hand-rolled IDesignTimeDbContextFactory building its own DbContextOptionsBuilder - a
        // migration could get scaffolded against PascalCase while the running application still
        // expects snake_case, and nothing before this test would notice.
        //
        // This has already caught one real divergence. Configuring schema operations without an
        // NpgsqlDataSource leaves the provider without the MapEnum registration it resolves an enum's
        // store type from, so the status column scaffolded as integer while the running application
        // expected task_item_status - and the migration still created the unused type.
        var designTimeOptions = new DbContextOptionsBuilder<ApplicationDbContext>();
        ApplicationDbContextOptions.ConfigureForSchemaOperations(designTimeOptions, ModelFixture.ConnectionString);

        using var designTimeContext = new ApplicationDbContext(designTimeOptions.Options);

        // Both sides read the design-time model rather than context.Model. EF strips the
        // Npgsql:Enum: annotation from the runtime model, so comparing runtime models would leave the
        // enums section empty on both sides: the part of this projection most likely to diverge would
        // agree trivially, and the assertion would be weaker than it reads.
        Describe(designTimeContext.GetService<IDesignTimeModel>().Model)
            .ShouldBe(Describe(_fixture.DesignTimeModel));
    }

    // A single formatted string, deliberately, rather than comparing nested objects that hold arrays:
    // arrays compare by reference, so two structurally identical projections built from two different
    // DbContext instances would never be equal that way. A string also gives a readable diff on failure.
    private static string Describe(IModel model)
    {
        var entityType = model.FindEntityType(typeof(TaskItem))
            ?? throw new InvalidOperationException($"{nameof(TaskItem)} is not part of the model.");
        var table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
            ?? throw new InvalidOperationException($"{nameof(TaskItem)} is not mapped to a table.");

        var columns = entityType.GetProperties()
            .Select(property =>
                $"{property.GetColumnName(table)}:{property.GetColumnType()}"
                + $":nullable={property.IsNullable}:maxLength={property.GetMaxLength()?.ToString() ?? "-"}")
            .OrderBy(line => line, StringComparer.Ordinal);

        var indexNames = entityType.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .OrderBy(name => name, StringComparer.Ordinal);

        var enumLabels = model.GetPostgresEnums()
            .Select(postgresEnum =>
                $"{postgresEnum.Schema}.{postgresEnum.Name}=[{string.Join(",", postgresEnum.Labels)}]")
            .OrderBy(line => line, StringComparer.Ordinal);

        var lines = new[] { $"table={entityType.GetTableName()}", "columns:" }
            .Concat(columns)
            .Append("indexes:")
            .Concat(indexNames)
            .Append("enums:")
            .Concat(enumLabels);

        return string.Join(Environment.NewLine, lines);
    }
}

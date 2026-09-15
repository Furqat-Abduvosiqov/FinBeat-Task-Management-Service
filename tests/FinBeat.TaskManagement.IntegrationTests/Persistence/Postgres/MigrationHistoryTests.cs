using FinBeat.TaskManagement.Infrastructure.Persistence;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class MigrationHistoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Applied_migrations_are_recorded_in_a_snake_case_history_table()
    {
        var tables = await ReadPublicTableNamesAsync();

        // Left alone, this is the one table in the schema that is not snake_case: EF names the
        // default at a configuration source the naming plugin is not allowed to override, while the
        // plugin still rewrites the columns inside it.
        tables.ShouldContain(ApplicationDbContext.MigrationsHistoryTableName);
        tables.ShouldAllBe(name => name == name.ToLowerInvariant());
    }

    private async Task<List<string>> ReadPublicTableNamesAsync()
    {
        await using var command = fixture.DataSource.CreateCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'");

        await using var reader = await command.ExecuteReaderAsync();

        var names = new List<string>();

        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}

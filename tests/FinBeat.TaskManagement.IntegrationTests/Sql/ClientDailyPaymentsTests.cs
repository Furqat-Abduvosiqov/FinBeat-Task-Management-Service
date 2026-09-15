using System.Globalization;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;

namespace FinBeat.TaskManagement.IntegrationTests.Sql;

/// <summary>Runs Задание 2 against a real PostgreSQL, exactly as the repository ships it.</summary>
/// <remarks>
/// The script under sql/postgresql is applied verbatim rather than restated here, so editing it is
/// what these assertions are about. The data and the two expected results are the assignment's own.
/// </remarks>
[Trait("Category", "RequiresDocker")]
public sealed class ClientDailyPaymentsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage(TestImages.PostgreSql)
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await ExecuteAsync(await File.ReadAllTextAsync(
            RepositoryRoot.Combine("sql", "postgresql", "client_daily_payments.sql")));

        // The rows the assignment tabulates, with its own ids.
        await ExecuteAsync(
            """
            INSERT INTO client.payments (client_id, dt, amount) VALUES
                (1, '2022-01-03 17:24:00', 100),
                (1, '2022-01-05 17:24:14', 200),
                (1, '2022-01-05 18:23:34', 250),
                (1, '2022-01-07 10:12:38', 50),
                (2, '2022-01-05 17:24:14', 278),
                (2, '2022-01-10 12:39:29', 300);
            """);
    }

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Theory]
    // Результат работы функции №1 and №2, read straight off the assignment.
    [InlineData(1, "2022-01-02", "2022-01-07", "0,100,0,450,0,50")]
    [InlineData(2, "2022-01-04", "2022-01-11", "0,278,0,0,0,0,300,0")]
    public async Task The_assignment_worked_examples_come_back_exactly(
        long clientId,
        string from,
        string to,
        string expected)
    {
        var page = await DailyPaymentsAsync(clientId, from, to);

        string.Join(',', page.Select(day => day.Amount.ToString("0.##", CultureInfo.InvariantCulture)))
            .ShouldBe(expected);
    }

    [Fact]
    public async Task Every_day_in_the_interval_is_returned_in_order_including_the_empty_ones()
    {
        var page = await DailyPaymentsAsync(1, "2022-01-02", "2022-01-07");

        page.Select(day => day.Date).ShouldBe(
        [
            new DateOnly(2022, 1, 2), new DateOnly(2022, 1, 3), new DateOnly(2022, 1, 4),
            new DateOnly(2022, 1, 5), new DateOnly(2022, 1, 6), new DateOnly(2022, 1, 7)
        ]);
    }

    [Fact]
    public async Task Two_payments_on_one_day_are_summed()
    {
        // 200 and 250 on the 5th. A day is a whole day, not the instant a payment landed.
        var page = await DailyPaymentsAsync(1, "2022-01-05", "2022-01-05");

        page.ShouldHaveSingleItem().Amount.ShouldBe(450m);
    }

    [Fact]
    public async Task A_client_with_no_payments_at_all_still_gets_a_row_per_day()
    {
        var page = await DailyPaymentsAsync(999, "2022-01-01", "2022-01-03");

        page.Count.ShouldBe(3);
        page.ShouldAllBe(day => day.Amount == 0m);
    }

    [Fact]
    public async Task An_interval_spanning_years_returns_every_day_of_it()
    {
        // "Интервалы дат могут охватывать несколько лет." Five years, two of them leap.
        var page = await DailyPaymentsAsync(1, "2020-01-01", "2024-12-31");

        page.Count.ShouldBe(1827);
        page[0].Date.ShouldBe(new DateOnly(2020, 1, 1));
        page[^1].Date.ShouldBe(new DateOnly(2024, 12, 31));
    }

    [Fact]
    public async Task An_interval_that_ends_before_it_starts_returns_nothing()
    {
        (await DailyPaymentsAsync(1, "2022-01-07", "2022-01-02")).ShouldBeEmpty();
    }

    private async Task<IReadOnlyList<(DateOnly Date, decimal Amount)>> DailyPaymentsAsync(
        long clientId,
        string from,
        string to)
    {
        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT dt, amount FROM client.get_daily_payments(@client, @from, @to)",
            connection);

        command.Parameters.AddWithValue("client", clientId);
        command.Parameters.AddWithValue("from", DateOnly.Parse(from, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("to", DateOnly.Parse(to, CultureInfo.InvariantCulture));

        var days = new List<(DateOnly, decimal)>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            days.Add((reader.GetFieldValue<DateOnly>(0), reader.GetDecimal(1)));
        }

        return days;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

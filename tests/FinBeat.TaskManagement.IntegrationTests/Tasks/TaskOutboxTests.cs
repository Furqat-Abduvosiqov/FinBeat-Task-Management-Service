using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Contracts.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Tasks;

/// <summary>Checks that a use case hands its event to the outbox instead of to the broker.</summary>
/// <remarks>Everything is resolved from <see cref="DependencyInjection.AddInfrastructure"/>, so this exercises the real MassTransit registration; no broker is running, which is the point.</remarks>
[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskOutboxTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Creating_a_task_writes_its_event_to_the_outbox()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConfiguration.ForDatabase(fixture.ConnectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateTaskHandler>();

        await using var provider = services.BuildServiceProvider();

        Guid taskId;

        using (var scope = provider.CreateScope())
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<CreateTaskHandler>()
                .HandleAsync(new CreateTaskCommand("Reach the outbox", null));

            result.IsSuccess.ShouldBeTrue();
            taskId = result.Value.Id;
        }

        await using var command = fixture.DataSource.CreateCommand(
            "SELECT message_type FROM outbox_message WHERE body LIKE @body");
        command.Parameters.AddWithValue("body", $"%{taskId}%");

        var messageType = (string?)await command.ExecuteScalarAsync();

        messageType.ShouldNotBeNull();
        messageType.ShouldContain(nameof(TaskCreated));
    }
}

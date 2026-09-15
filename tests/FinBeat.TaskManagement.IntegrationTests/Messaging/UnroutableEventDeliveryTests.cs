using FinBeat.TaskManagement.Contracts.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shouldly;
using Testcontainers.RabbitMq;

namespace FinBeat.TaskManagement.IntegrationTests.Messaging;

/// <summary>Proves against a real broker that an event nothing is bound to receive is kept, not dropped.</summary>
/// <remarks>What MassTransit intends to declare is not what the broker ends up holding, and only a real broker shows the difference.</remarks>
[Trait("Category", "RequiresDocker")]
public sealed class UnroutableEventDeliveryTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _broker = new RabbitMqBuilder()
        .WithImage(TestImages.RabbitMq)
        .Build();

    public Task InitializeAsync() => _broker.StartAsync();

    public Task DisposeAsync() => _broker.DisposeAsync().AsTask();

    [Fact]
    public async Task An_event_published_with_no_consumer_bound_waits_in_the_unroutable_queue()
    {
        await using var provider = BuildProvider();
        var bus = provider.GetRequiredService<IBusControl>();

        // No consumer is registered, so nothing declares a queue for TaskCreated. Before the alternate
        // exchange this publish succeeded and the event was discarded by the broker without a word.
        await bus.StartAsync(CancellationToken.None);

        try
        {
            // The queue has to exist before the first publish, not because of it: this service always
            // publishes through the transactional outbox, and the outbox send path declares only the
            // exchange it is sending to. Deploying the publish topology at startup is what puts the
            // alternate exchange and its queue on the broker in time to catch anything.
            (await UnroutableQueueExistsAsync()).ShouldBeTrue();

            await bus.Publish(new TaskCreated(Guid.NewGuid(), "Nobody is listening", "", "New", DateTimeOffset.UtcNow));

            (await ReadUnroutableAsync()).ShouldNotBeNull();
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>Asks the broker whether the queue is there, without creating it.</summary>
    private async Task<bool> UnroutableQueueExistsAsync()
    {
        await using var connection = await ConnectAsync();
        await using var channel = await connection.CreateChannelAsync();

        try
        {
            // A passive declare is the only way to ask; it faults the channel when the queue is absent.
            await channel.QueueDeclarePassiveAsync(DependencyInjection.UnroutableName);
            return true;
        }
        catch (OperationInterruptedException)
        {
            return false;
        }
    }

    /// <summary>Polls the unroutable queue, since a publish returns before the broker has routed it.</summary>
    private async Task<BasicGetResult?> ReadUnroutableAsync()
    {
        await using var connection = await ConnectAsync();
        await using var channel = await connection.CreateChannelAsync();

        for (var attempt = 0; attempt < 50; attempt++)
        {
            var message = await channel.BasicGetAsync(DependencyInjection.UnroutableName, autoAck: true);

            if (message is not null)
            {
                return message;
            }

            await Task.Delay(100);
        }

        return null;
    }

    private Task<IConnection> ConnectAsync() =>
        new ConnectionFactory { Uri = new Uri(_broker.GetConnectionString()) }.CreateConnectionAsync();

    private ServiceProvider BuildProvider()
    {
        // The container generates its own credentials; they arrive in the connection string.
        var broker = new Uri(_broker.GetConnectionString());
        var credentials = broker.UserInfo.Split(':');

        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [TestConfiguration.ConnectionStringKey] = TestConfiguration.UnusedConnectionString,
            [$"{DependencyInjection.RabbitMqSectionName}:Host"] = broker.Host,
            [$"{DependencyInjection.RabbitMqSectionName}:Port"] = broker.Port.ToString(),
            [$"{DependencyInjection.RabbitMqSectionName}:User"] = credentials[0],
            [$"{DependencyInjection.RabbitMqSectionName}:Pass"] = credentials[1]
        }).Build());

        return services.BuildServiceProvider();
    }
}

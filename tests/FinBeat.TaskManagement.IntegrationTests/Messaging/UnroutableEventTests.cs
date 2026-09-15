using System.Reflection;
using FinBeat.TaskManagement.Infrastructure;
using MassTransit;
using MassTransit.RabbitMqTransport.Topology;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Messaging;

/// <summary>Proves an event that no consumer queue is bound to receive is diverted rather than dropped.</summary>
/// <remarks>Reads the topology MassTransit would declare, so no broker is needed. The failure guarded against is a silent one.</remarks>
public sealed class UnroutableEventTests
{
    private const string AlternateExchangeArgument = "alternate-exchange";

    [Fact]
    public async Task Every_contract_diverts_its_unroutable_events_to_a_durable_queue()
    {
        await using var provider = BuildProvider();
        var bus = provider.GetRequiredService<IBusControl>();

        // An empty list would pass every assertion below without testing anything.
        DependencyInjection.IntegrationEventTypes.ShouldNotBeEmpty();

        foreach (var integrationEvent in DependencyInjection.IntegrationEventTypes)
        {
            var topology = BrokerTopologyFor(bus, integrationEvent);

            topology.Exchanges.ShouldContain(
                exchange => exchange.ExchangeArguments.ContainsKey(AlternateExchangeArgument)
                    && Equals(exchange.ExchangeArguments[AlternateExchangeArgument], DependencyInjection.UnroutableName),
                $"{integrationEvent.Name} is published to an exchange with no alternate exchange");

            // An alternate exchange with nothing bound behind it drops the message just as quietly.
            topology.QueueBindings.ShouldContain(
                binding => binding.Source.ExchangeName == DependencyInjection.UnroutableName
                    && binding.Destination.QueueName == DependencyInjection.UnroutableName
                    && binding.Destination.Durable,
                $"{integrationEvent.Name} has no durable queue behind its alternate exchange");
        }
    }

    private static BrokerTopology BrokerTopologyFor(IBus bus, Type integrationEvent) =>
        (BrokerTopology)typeof(UnroutableEventTests)
            .GetMethod(nameof(BrokerTopologyOf), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(integrationEvent)
            .Invoke(null, [bus])!;

    private static BrokerTopology BrokerTopologyOf<TEvent>(IBus bus)
        where TEvent : class =>
        ((IRabbitMqMessagePublishTopology<TEvent>)bus.Topology.Publish<TEvent>()).GetBrokerTopology();

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConfiguration.ForDatabase(TestConfiguration.UnusedConnectionString));

        return services.BuildServiceProvider();
    }
}

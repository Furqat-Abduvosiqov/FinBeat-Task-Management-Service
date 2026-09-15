using FinBeat.TaskManagement.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Messaging;

/// <summary>Proves the broker address comes from configuration rather than from a hard-coded fallback.</summary>
/// <remarks>Only <c>StartAsync</c> dials the broker, so neither test needs RabbitMQ running. Disposal has to be asynchronous: the bus creates MassTransit's usage tracker, which is <see cref="IAsyncDisposable"/> only.</remarks>
public sealed class RabbitMqRegistrationTests
{
    [Fact]
    public async Task Bus_address_is_bound_from_the_RabbitMq_configuration_section()
    {
        await using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [$"{DependencyInjection.RabbitMqSectionName}:Host"] = "broker.internal",
            [$"{DependencyInjection.RabbitMqSectionName}:Port"] = "5673",
            [$"{DependencyInjection.RabbitMqSectionName}:VHost"] = "finbeat"
        });

        var address = provider.GetRequiredService<IBusControl>().Address;

        address.GetLeftPart(UriPartial.Authority).ShouldBe("rabbitmq://broker.internal:5673");
        address.AbsolutePath.ShouldStartWith("/finbeat/");
    }

    [Fact]
    public async Task Bus_falls_back_to_the_MassTransit_defaults_when_the_section_is_absent()
    {
        // No section, and no `?? "localhost"` of our own - the defaults already say localhost:5672.
        await using var provider = BuildProvider([]);

        var address = provider.GetRequiredService<IBusControl>().Address;

        // No port in the address: MassTransit elides 5672, its own default.
        address.GetLeftPart(UriPartial.Authority).ShouldBe("rabbitmq://localhost");
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        settings[TestConfiguration.ConnectionStringKey] = TestConfiguration.UnusedConnectionString;

        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        return services.BuildServiceProvider();
    }
}

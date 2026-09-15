using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using FinBeat.TaskManagement.IntegrationTests.Persistence.Model;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Messaging;

/// <summary>Proves the broker address comes from configuration rather than from a hard-coded fallback.</summary>
/// <remarks>
/// Resolving the bus builds its topology and address; only <c>StartAsync</c> would dial the broker,
/// so neither test needs RabbitMQ running. Disposal must be asynchronous: resolving the bus creates
/// MassTransit's usage tracker, which implements only <see cref="IAsyncDisposable"/>.
/// </remarks>
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
        // No section, and no `?? "localhost"` of our own: RabbitMqTransportOptions already defaults to
        // localhost:5672 on the root virtual host, which is what a developer machine wants.
        await using var provider = BuildProvider([]);

        var address = provider.GetRequiredService<IBusControl>().Address;

        // No port in the address: MassTransit elides 5672, its own default.
        address.GetLeftPart(UriPartial.Authority).ShouldBe("rabbitmq://localhost");
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        settings[$"{DatabaseOptions.SectionName}:{DatabaseOptions.ConnectionStringName}"] =
            ModelFixture.ConnectionString;

        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        return services.BuildServiceProvider();
    }
}

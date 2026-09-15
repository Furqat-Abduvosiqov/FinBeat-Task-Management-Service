using FinBeat.TaskManagement.Contracts.Tasks;
using FinBeat.TaskManagement.Listener.Consumers;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace FinBeat.TaskManagement.Listener;

/// <summary>Composes the listener host, so that Program.cs stays a readable outline of startup.</summary>
/// <remarks>A second copy of the API's bootstrap on purpose: this is a separate deployable whose only permitted reference is Contracts, and Contracts is dependency-free so it cannot hold hosting code.</remarks>
internal static class Bootstrap
{
    // Half of a cross-deployable contract: the API binds the same section name to reach the same broker.
    private const string RabbitMqSectionName = "RabbitMq";

    private const string OpenTelemetrySection = "OpenTelemetry";

    private const string ServiceNameKey = "ServiceName";

    private const string OtlpEndpointKey = "OtlpEndpoint";

    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    // MassTransit emits its own ActivitySource, so subscribing needs the name and no extra package.
    private const string MassTransitActivitySource = "MassTransit";

    // The other half of the alternate-exchange contract: binding a queue re-declares the API's
    // exchange, and a mismatched argument is PRECONDITION_FAILED and a listener that never starts.
    private const string UnroutableName = "unroutable";

    /// <summary>A console logger for the window before configuration is read, so a failure while building the host is not lost.</summary>
    internal static Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

    /// <summary>Registers logging, tracing and the hosted services this listener runs.</summary>
    internal static HostApplicationBuilder AddListenerHost(this HostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services));

        builder.AddTelemetry();
        builder.AddMessaging();

        return builder;
    }

    private static void AddMessaging(this HostApplicationBuilder builder)
    {
        // The same options type and the same endpoint name formatter the publisher uses, so the
        // queues this binds are the ones the API's exchanges deliver to.
        builder.Services.AddOptions<RabbitMqTransportOptions>()
            .Bind(builder.Configuration.GetSection(RabbitMqSectionName));

        builder.Services.AddMassTransit(bus =>
        {
            // Same outbound call as the API suppresses, for the same reason.
            bus.DisableUsageTelemetry();

            bus.SetKebabCaseEndpointNameFormatter();
            // Named rather than assembly-scanned: the scan reads exported types only, and these are internal.
            bus.AddConsumer<TaskCreatedConsumer>();
            bus.AddConsumer<TaskDetailsUpdatedConsumer>();
            bus.AddConsumer<TaskStatusChangedConsumer>();
            bus.AddConsumer<TaskDeletedConsumer>();

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.DeployPublishTopology = true;

                foreach (var integrationEvent in typeof(TaskCreated).Assembly.GetExportedTypes())
                {
                    rabbit.Publish(integrationEvent, exchange => exchange.BindAlternateExchangeQueue(UnroutableName));
                }

                rabbit.ConfigureEndpoints(context);
            });
        });
    }

    private static void AddTelemetry(this HostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(OpenTelemetrySection);
        var serviceName = section[ServiceNameKey] ?? builder.Environment.ApplicationName;
        var otlpEndpoint = section[OtlpEndpointKey] ?? Environment.GetEnvironmentVariable(OtlpEndpointVariable);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(MassTransitActivitySource);

                // Opt in: with no endpoint configured every span would fail against localhost:4317.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            });
    }
}

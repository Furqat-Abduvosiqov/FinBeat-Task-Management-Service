using FinBeat.TaskManagement.Contracts.Messaging;
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
    private const string OpenTelemetrySection = "OpenTelemetry";

    private const string ServiceNameKey = "ServiceName";

    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    // MassTransit emits its own ActivitySource, so subscribing needs the name and no extra package.
    private const string MassTransitActivitySource = "MassTransit";

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
        // Both broker names come from Contracts.Messaging.BrokerTopology: the API declares the same
        // section and the same alternate exchange from that one place, so a rename cannot go stale
        // on just one side and surface as PRECONDITION_FAILED at broker-declare time.
        builder.Services.AddOptions<RabbitMqTransportOptions>()
            .Bind(builder.Configuration.GetSection(BrokerTopology.RabbitMqSectionName));

        builder.Services.AddMassTransit(bus =>
        {
            bus.DisableUsageTelemetry();

            bus.SetKebabCaseEndpointNameFormatter();
            
            bus.AddConsumer<TaskCreatedConsumer>();
            bus.AddConsumer<TaskDetailsUpdatedConsumer>();
            bus.AddConsumer<TaskStatusChangedConsumer>();
            bus.AddConsumer<TaskDeletedConsumer>();

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.DeployPublishTopology = true;

                foreach (var integrationEvent in typeof(TaskCreated).Assembly.GetExportedTypes())
                {
                    rabbit.Publish(
                        integrationEvent,
                        exchange => exchange.BindAlternateExchangeQueue(BrokerTopology.UnroutableName));
                }

                rabbit.ConfigureEndpoints(context);
            });
        });
    }

    private static void AddTelemetry(this HostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(OpenTelemetrySection);
        var serviceName = section[ServiceNameKey] ?? builder.Environment.ApplicationName;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(MassTransitActivitySource);

                // The exporter reads OTEL_EXPORTER_OTLP_ENDPOINT itself; skip registering it
                // entirely when unset, or every span fails against its localhost:4317 default.
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OtlpEndpointVariable)))
                {
                    tracing.AddOtlpExporter();
                }
            });
    }
}

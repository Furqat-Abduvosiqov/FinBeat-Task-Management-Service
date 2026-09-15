using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace FinBeat.TaskManagement.Listener;

/// <summary>Composes the listener host, so that Program.cs stays a readable outline of startup.</summary>
/// <remarks>
/// Deliberately a second copy of the API's bootstrap rather than a shared one. This service is a
/// separate deployable and may share only the wire format with the API, so the only project it can
/// reference is Contracts - which is itself dependency-free and so cannot hold hosting code.
/// </remarks>
internal static class Bootstrap
{
    private const string OpenTelemetrySection = "OpenTelemetry";

    private const string ServiceNameKey = "ServiceName";

    private const string OtlpEndpointKey = "OtlpEndpoint";

    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    // MassTransit emits its own ActivitySource, so subscribing needs the name and no extra package.
    private const string MassTransitActivitySource = "MassTransit";

    /// <summary>A console logger for the window before configuration has been read.</summary>
    internal static Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

    /// <summary>Registers logging, tracing and the hosted services this listener runs.</summary>
    internal static HostApplicationBuilder AddListenerHost(this HostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services));

        builder.AddTelemetry();

        builder.Services.AddHostedService<Worker>();

        return builder;
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
                tracing.AddHttpClientInstrumentation()
                    .AddSource(MassTransitActivitySource);

                // Opt in: with no endpoint configured every span would fail against localhost:4317.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            });
    }
}

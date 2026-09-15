using FinBeat.TaskManagement.Infrastructure;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace FinBeat.TaskManagement.Api;

/// <summary>Composes the API host, so that Program.cs stays a readable outline of startup.</summary>
internal static class Bootstrap
{
    private const string OpenTelemetrySection = "OpenTelemetry";

    private const string ServiceNameKey = "ServiceName";

    private const string OtlpEndpointKey = "OtlpEndpoint";

    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    // MassTransit emits its own ActivitySource, so subscribing needs the name and no extra package.
    private const string MassTransitActivitySource = "MassTransit";

    /// <summary>A console logger for the window before configuration has been read.</summary>
    /// <remarks>Without it, a failure while building the host would be lost entirely.</remarks>
    internal static Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

    /// <summary>Registers logging, tracing, Swagger and the infrastructure adapters.</summary>
    internal static WebApplicationBuilder AddApiHost(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

        builder.AddTelemetry();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>Builds the request pipeline.</summary>
    internal static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        return app;
    }

    private static void AddTelemetry(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(OpenTelemetrySection);
        var serviceName = section[ServiceNameKey] ?? builder.Environment.ApplicationName;
        var otlpEndpoint = section[OtlpEndpointKey] ?? Environment.GetEnvironmentVariable(OtlpEndpointVariable);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddSource(MassTransitActivitySource);

                // Opt in: with no endpoint configured every span would fail against localhost:4317.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            });
    }
}

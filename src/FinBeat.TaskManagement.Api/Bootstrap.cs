using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Application.Tasks.Queries;
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

    /// <summary>A console logger for the window before configuration is read, so a failure while building the host is not lost.</summary>
    internal static Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

    /// <summary>Registers logging, tracing, error handling, Swagger and the infrastructure adapters.</summary>
    internal static WebApplicationBuilder AddApiHost(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

        builder.AddTelemetry();

        // Without AddProblemDetails the handler has nothing to write through and the body comes back empty.
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddUseCases();

        return builder;
    }

    // The use cases are registered from the host rather than from Application, which may reference
    // EF Core and nothing else - IServiceCollection included.
    private static void AddUseCases(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<UpdateTaskDetailsHandler>();
        services.AddScoped<ChangeTaskStatusHandler>();
        services.AddScoped<DeleteTaskHandler>();
        services.AddScoped<GetTaskByIdHandler>();
        services.AddScoped<GetTasksHandler>();
    }

    /// <summary>Builds the request pipeline.</summary>
    internal static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
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

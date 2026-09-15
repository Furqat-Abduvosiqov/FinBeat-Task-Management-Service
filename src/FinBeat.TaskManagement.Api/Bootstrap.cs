using System.Reflection;
using System.Text.Json.Serialization;
using FinBeat.TaskManagement.Api.Endpoints;
using FinBeat.TaskManagement.Application.Tasks;
using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Application.Tasks.Queries;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        // Statuses travel as names in both directions. Without this they bind as numbers on the way
        // in while responses and events keep emitting names, so a client cannot echo back what it read.
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddApiDocumentation();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddUseCases();

        return builder;
    }

    private static void AddApiDocumentation(this IServiceCollection services) =>
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FinBeat Task Management",
                Version = "v1",
                Description =
                    "Create, read, update and delete tasks. Every change is published as an integration "
                    + "event through a transactional outbox, so an event is never sent for a change that "
                    + "rolled back. Failures come back as RFC 9457 problem details carrying a stable "
                    + "`code` extension, which is the part worth matching on.",
            });

            // The request bodies. A query parameter needs stating at the endpoint instead - see ListAsync.
            options.MapType<TaskItemStatus>(OpenApiConventions.StatusSchema);

            options.OperationFilter<StatusParameterFilter>();
            options.SupportNonNullableReferenceTypes();

            // The XML from this assembly and from Application, which owns TaskResponse.
            IncludeXmlComments(options, Assembly.GetExecutingAssembly());
            IncludeXmlComments(options, typeof(TaskResponse).Assembly);
        });

    private static void IncludeXmlComments(SwaggerGenOptions options, Assembly assembly)
    {
        var documentation = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

        if (File.Exists(documentation))
        {
            options.IncludeXmlComments(documentation);
        }
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
        app.MapTaskEndpoints();

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

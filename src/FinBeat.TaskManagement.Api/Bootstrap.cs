using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using FinBeat.TaskManagement.Api.Endpoints;
using FinBeat.TaskManagement.Api.Endpoints.Validation;
using FinBeat.TaskManagement.Application.Tasks;
using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Application.Tasks.Queries;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using FluentValidation;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace FinBeat.TaskManagement.Api;

/// <summary>Composes the API host, so that Program.cs stays a readable outline of startup.</summary>
internal static class Bootstrap
{

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

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        // Query/route enums already bind from names via Enum.TryParse; this is what makes a JSON
        // body - request or response - carry names instead of numbers on the wire. allowIntegerValues
        // is off because the document advertises a string enum, and accepting numbers anyway would
        // make it lie about what the API takes.
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)));

        builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>(includeInternalTypes: true);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddApiDocumentation();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddUseCases();

        // AddMassTransit already registered a bus health check; this exposes it. No DbContext check
        // alongside it - AddDbContextCheck opens a fresh connection on every probe, and compose
        // already gates the API's startup on postgres being healthy before it runs at all.
        builder.Services.AddHealthChecks();

        return builder;
    }

    private static void AddApiDocumentation(this IServiceCollection services)
    {
        // Swashbuckle builds its schema from Mvc's JsonOptions, not the minimal-API JsonOptions
        // ConfigureHttpJsonOptions sets above - without this the wire carries names but the document
        // still shows numbers.
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)));

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

            options.SupportNonNullableReferenceTypes();
            
            options.IncludeXmlComments(Assembly.GetExecutingAssembly());
            options.IncludeXmlComments(typeof(TaskResponse).Assembly);
            options.IncludeXmlComments(typeof(TaskItemStatus).Assembly);
        });
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
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapHealthChecks("/health").ExcludeFromDescription();
        app.MapTaskEndpoints();

        return app;
    }

    private static void AddTelemetry(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection("OpenTelemetry");
        var serviceName = section["ServiceName"] ?? builder.Environment.ApplicationName;
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddSource("MassTransit");

                // AddOtlpExporter() reads this same variable itself; checking it here first keeps
                // the opt-in - no endpoint means no exporter, not one failing against the SDK's
                // localhost:4317 default.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter();
                }
            });
    }
}

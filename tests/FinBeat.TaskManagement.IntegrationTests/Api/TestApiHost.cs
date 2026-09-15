using System.Text.Json;
using FinBeat.TaskManagement.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace FinBeat.TaskManagement.IntegrationTests.Api;

/// <summary>Starts the API the way the real host does, on an in-memory server.</summary>
/// <remarks>Composed through <see cref="Bootstrap.AddApiHost"/> and <see cref="Bootstrap.UseApiPipeline"/> rather than a hand-built pipeline, so a change to either is a change every test here sees.</remarks>
internal static class TestApiHost
{
    /// <summary>Builds and starts the host. The caller owns disposal.</summary>
    /// <param name="environment">The environment name, which decides Swagger and the developer exception page.</param>
    /// <param name="connectionString">The database to point at, or null for one that is never dialled.</param>
    /// <param name="configureBuilder">Extra registrations, applied after the host's own.</param>
    /// <param name="configureApp">Extra routes, applied after the pipeline is built.</param>
    internal static async Task<WebApplication> StartAsync(
        string environment,
        string? connectionString = null,
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<WebApplication>? configureApp = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });

        builder.WebHost.UseTestServer();
        builder.Configuration.AddConfiguration(
            TestConfiguration.ForDatabase(connectionString ?? TestConfiguration.UnusedConnectionString));

        builder.AddApiHost();
        configureBuilder?.Invoke(builder);

        var app = builder.Build();
        app.UseApiPipeline();
        configureApp?.Invoke(app);

        await app.StartAsync();

        return app;
    }

    /// <summary>Reads a response body as JSON.</summary>
    /// <param name="response">The response to read.</param>
    internal static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}

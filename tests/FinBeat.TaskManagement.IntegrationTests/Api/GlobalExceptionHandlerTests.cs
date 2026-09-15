using System.Collections.Concurrent;
using System.Net;
using FinBeat.TaskManagement.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Serilog.Events;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Api;

/// <summary>Drives a throwing endpoint through the real pipeline that <see cref="Bootstrap.UseApiPipeline"/> composes.</summary>
/// <remarks>An in-memory server, so no port is bound and no database or broker is reached.</remarks>
public sealed class GlobalExceptionHandlerTests
{
    private const string ThrowingRoute = "/boom";

    // Distinctive enough that finding it in a response body means the exception leaked.
    private const string ExceptionMessage = "Host=db.internal;Password=hunter2";

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Unhandled_exception_becomes_a_500_problem_details_response(string environment)
    {
        // Development matters: WebApplication inserts the developer exception page there, and this
        // handler has to win because it sits inside it.
        await using var app = await StartAsync(environment);

        var response = await app.GetTestClient().GetAsync(ThrowingRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");

        var problem = await response.ReadJsonAsync();
        problem.GetProperty("status").GetInt32().ShouldBe(500);
        problem.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Response_body_carries_nothing_from_the_exception()
    {
        await using var app = await StartAsync(Environments.Development);

        var body = await (await app.GetTestClient().GetAsync(ThrowingRoute)).Content.ReadAsStringAsync();

        body.ShouldNotContain(ExceptionMessage);
        body.ShouldNotContain(nameof(InvalidOperationException));
        body.ShouldNotContain(nameof(GlobalExceptionHandlerTests));
    }

    [Fact]
    public async Task Exception_reaches_the_log_although_it_never_reaches_the_client()
    {
        // Why the handler logs nothing of its own: ExceptionHandlerMiddleware already does.
        var sink = new CapturingSink();
        await using var app = await StartAsync(Environments.Production, sink);

        await app.GetTestClient().GetAsync(ThrowingRoute);

        sink.Events.ShouldContain(logged =>
            logged.Level == LogEventLevel.Error
            && logged.Exception is InvalidOperationException
            && logged.Exception.Message == ExceptionMessage);
    }

    private static Task<WebApplication> StartAsync(string environment, ILogEventSink? sink = null) =>
        TestApiHost.StartAsync(
            environment,
            // ReadFrom.Services picks the sink up, so the host logs through its own pipeline.
            configureBuilder: builder =>
            {
                if (sink is not null)
                {
                    builder.Services.AddSingleton(sink);
                }
            },
            configureApp: app =>
                app.MapGet(ThrowingRoute, void () => throw new InvalidOperationException(ExceptionMessage)));

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> _events = new();

        internal IEnumerable<LogEvent> Events => _events;

        public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
    }
}

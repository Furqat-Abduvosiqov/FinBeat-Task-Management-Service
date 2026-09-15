using System.Text.Json;
using System.Text.RegularExpressions;
using FinBeat.TaskManagement.Api;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Api;

/// <summary>Fetches the generated OpenAPI document from the composed host.</summary>
/// <remarks>Reaches no database: generating the document needs the routes and the schemas, not a connection.</remarks>
public sealed class ApiDocumentationTests
{
    [Fact]
    public async Task The_document_describes_every_task_endpoint_with_the_statuses_it_answers()
    {
        await using var app = await StartAsync();

        var document = await ReadDocumentAsync(app);
        var paths = document.GetProperty("paths");

        document.GetProperty("info").GetProperty("title").GetString().ShouldBe("FinBeat Task Management");

        ShouldDescribe(paths, "/tasks", "post", "CreateTask", "201", "400");
        ShouldDescribe(paths, "/tasks", "get", "GetTasks", "200", "400");
        ShouldDescribe(paths, "/tasks/{id}", "get", "GetTaskById", "200", "404");
        ShouldDescribe(paths, "/tasks/{id}", "put", "UpdateTaskDetails", "200", "400", "404");
        ShouldDescribe(paths, "/tasks/{id}", "delete", "DeleteTask", "204", "404");
        ShouldDescribe(paths, "/tasks/{id}/status", "put", "ChangeTaskStatus", "200", "400", "404", "409");
    }

    [Fact]
    public async Task Statuses_are_documented_as_names_on_both_the_request_and_the_query()
    {
        // Swashbuckle infers schemas with its own serializer, so without the explicit mapping these
        // would read as integers while the wire carries names.
        await using var app = await StartAsync();

        var document = await ReadDocumentAsync(app);
        var expected = Enum.GetNames<TaskItemStatus>();

        var schemas = document.GetProperty("components").GetProperty("schemas");

        var query = document.GetProperty("paths").GetProperty("/tasks").GetProperty("get")
            .GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "status");

        // The converter gives every enum one named component that request bodies point at; a query
        // parameter never reaches it, so the filter states that schema inline.
        schemas.GetProperty("ChangeTaskStatusRequest").GetProperty("properties").GetProperty("status")
            .GetProperty("$ref").GetString().ShouldBe("#/components/schemas/TaskItemStatus");

        Names(schemas.GetProperty(nameof(TaskItemStatus))).ShouldBe(expected);
        Names(query.GetProperty("schema")).ShouldBe(expected);
        query.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task The_version_the_document_states_is_one_the_bundled_Swagger_UI_reads()
    {
        // Swashbuckle ships the generator and the UI as separate packages, and they can fall out of
        // step. Microsoft.OpenApi 1.6.23 began stamping documents "3.0.4" where it used to write
        // "3.0.1", while the swagger-ui bundled up to Swashbuckle 7.2.0 still tested that field with
        // ^3\.0\.([0123])(?:-rc[012])?$ - so a valid document rendered as nothing but "The provided
        // definition does not specify a valid version field". Asserting a literal version here would
        // only restate what the serialiser does; what is worth holding is that the two halves still
        // agree, so the UI's own test is read out of the bundle it serves and applied to the version.
        await using var app = await StartAsync();

        var version = (await ReadDocumentAsync(app)).GetProperty("openapi").GetString();
        var bundle = await app.GetTestClient().GetStringAsync("/swagger/swagger-ui-bundle.js");

        var tests = Regex.Matches(bundle, @"\^3\\\.0\\\.[^/\r\n]*?\$")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Were swagger-ui to stop shipping that test in this shape, the assertion below would hold
        // over nothing at all, so finding it is the first thing to establish.
        version.ShouldNotBeNullOrWhiteSpace();
        tests.ShouldNotBeEmpty();
        tests.ShouldAllBe(test => Regex.IsMatch(version, test));
    }

    private static string[] Names(JsonElement schema)
    {
        schema.GetProperty("type").GetString().ShouldBe("string");

        return schema.GetProperty("enum").EnumerateArray().Select(value => value.GetString()!).ToArray();
    }

    private static void ShouldDescribe(
        JsonElement paths,
        string path,
        string method,
        string operationId,
        params string[] responses)
    {
        var operation = paths.GetProperty(path).GetProperty(method);

        operation.GetProperty("operationId").GetString().ShouldBe(operationId);
        operation.GetProperty("summary").GetString().ShouldNotBeNullOrWhiteSpace();
        operation.GetProperty("responses").EnumerateObject()
            .Select(response => response.Name).Order().ToArray()
            .ShouldBe(responses);
    }

    private static async Task<JsonElement> ReadDocumentAsync(WebApplication app) =>
        await (await app.GetTestClient().GetAsync("/swagger/v1/swagger.json")).ReadJsonAsync();

    // Development, because that is the only environment the pipeline serves Swagger in.
    private static Task<WebApplication> StartAsync() => TestApiHost.StartAsync(Environments.Development);
}

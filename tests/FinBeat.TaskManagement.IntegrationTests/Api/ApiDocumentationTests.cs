using System.Text.Json;
using System.Text.RegularExpressions;
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
    public async Task Statuses_are_documented_as_names_the_enum_itself_explains()
    {
        // Statuses travel as names now, so there's no generated legend to keep in sync: the enum's
        // own <summary> reaches the schema through Swashbuckle's XML-comments support.
        await using var app = await StartAsync();

        var document = await ReadDocumentAsync(app);
        var expected = Enum.GetNames<TaskItemStatus>();

        var schemas = document.GetProperty("components").GetProperty("schemas");

        var query = document.GetProperty("paths").GetProperty("/tasks").GetProperty("get")
            .GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "status");

        // Requests and responses both point at the one named component, so both get the same names.
        schemas.GetProperty("ChangeTaskStatusRequest").GetProperty("properties").GetProperty("status")
            .GetProperty("$ref").GetString().ShouldBe("#/components/schemas/TaskItemStatus");
        schemas.GetProperty("TaskResponse").GetProperty("properties").GetProperty("status")
            .GetProperty("$ref").GetString().ShouldBe("#/components/schemas/TaskItemStatus");

        Values(schemas.GetProperty(nameof(TaskItemStatus))).ShouldBe(expected);

        // Swashbuckle points the parameter at the same component, so it inherits the names too.
        query.GetProperty("schema").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/TaskItemStatus");

        // Nothing appended to the enum's own <summary> - no per-value meaning left to spell out.
        schemas.GetProperty(nameof(TaskItemStatus)).GetProperty("description").GetString()
            .ShouldBe("Where a task is in its lifecycle.");

        // The endpoint still sets its own parameter description by hand: Swagger UI doesn't show a
        // $ref target's description next to the field.
        query.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private static string[] Values(JsonElement schema)
    {
        schema.GetProperty("type").GetString().ShouldBe("string");

        return schema.GetProperty("enum").EnumerateArray().Select(value => value.GetString()!).ToArray();
    }

    [Fact]
    public async Task The_version_the_document_states_is_one_the_bundled_Swagger_UI_reads()
    {
        // The generator and the bundled UI ship separately and can drift, once rendering a valid
        // document as "does not specify a valid version field" - so check against the UI's own
        // version regex instead of asserting a literal.
        await using var app = await StartAsync();

        var version = (await ReadDocumentAsync(app)).GetProperty("openapi").GetString();
        var bundle = await app.GetTestClient().GetStringAsync("/swagger/swagger-ui-bundle.js");

        var tests = Regex.Matches(bundle, @"\^3\\\.0\\\.[^/\r\n]*?\$")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // If swagger-ui stopped shipping that test in this shape, the assertion below would pass over
        // nothing - so check that some tests were actually found first.
        version.ShouldNotBeNullOrWhiteSpace();
        tests.ShouldNotBeEmpty();
        tests.ShouldAllBe(test => Regex.IsMatch(version, test));
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

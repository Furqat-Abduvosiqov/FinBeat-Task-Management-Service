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
    public async Task Statuses_are_documented_as_numbers_and_the_document_says_what_each_one_means()
    {
        // Statuses travel as numbers, which tells a reader nothing on its own. OpenAPI has no field
        // for documenting one enum member, so the meanings go in the description - read out of the
        // XML summaries the enum already carries, which is why they cannot drift from the code.
        await using var app = await StartAsync();

        var document = await ReadDocumentAsync(app);
        var expected = Enum.GetValues<TaskItemStatus>().Select(status => (int)status).ToArray();

        var schemas = document.GetProperty("components").GetProperty("schemas");

        var query = document.GetProperty("paths").GetProperty("/tasks").GetProperty("get")
            .GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "status");

        // Requests and responses both point at the one named component, so both get the meanings.
        schemas.GetProperty("ChangeTaskStatusRequest").GetProperty("properties").GetProperty("status")
            .GetProperty("$ref").GetString().ShouldBe("#/components/schemas/TaskItemStatus");
        schemas.GetProperty("TaskResponse").GetProperty("properties").GetProperty("status")
            .GetProperty("$ref").GetString().ShouldBe("#/components/schemas/TaskItemStatus");

        Values(schemas.GetProperty(nameof(TaskItemStatus))).ShouldBe(expected);

        // Swashbuckle points the parameter at the same component, so it inherits the values.
        query.GetProperty("schema").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/TaskItemStatus");

        ShouldExplainEveryStatus(schemas.GetProperty(nameof(TaskItemStatus)));
        // Swagger UI renders the parameter's own description, not the component's, so the
        // meanings have to reach the reader here too.
        ShouldExplainEveryStatus(query);

        // Swashbuckle writes the enum's own <summary> here first and EnumDocumentation.Describe has
        // to keep it; without this, dropping the `existing` branch is a silent loss.
        schemas.GetProperty(nameof(TaskItemStatus)).GetProperty("description").GetString()
            .ShouldNotBeNull()
            .Split('\n')[0].Trim()
            .ShouldBe("Where a task is in its lifecycle.");

        query.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>Asserts the description gives every status its number, its name and a meaning.</summary>
    private static void ShouldExplainEveryStatus(JsonElement schema)
    {
        var description = schema.GetProperty("description").GetString().ShouldNotBeNull();

        foreach (var status in Enum.GetValues<TaskItemStatus>())
        {
            // The meaning is the half that would quietly go missing if the XML stopped being read,
            // so the assertion is on the whole line rather than on the number and name alone.
            var line = description.Split('\n')
                .SingleOrDefault(candidate => candidate.StartsWith($"{(int)status} = {status}", StringComparison.Ordinal))
                .ShouldNotBeNull($"{status} is not explained by: {description}");

            line[$"{(int)status} = {status}".Length..].ShouldStartWith(" - ");
        }
    }

    private static int[] Values(JsonElement schema)
    {
        schema.GetProperty("type").GetString().ShouldBe("integer");

        return schema.GetProperty("enum").EnumerateArray().Select(value => value.GetInt32()).ToArray();
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

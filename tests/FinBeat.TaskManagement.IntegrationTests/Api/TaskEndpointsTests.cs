using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinBeat.TaskManagement.Api;
using FinBeat.TaskManagement.Api.Endpoints;
using FinBeat.TaskManagement.Api.Endpoints.Validation;
using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FinBeat.TaskManagement.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Api;

/// <summary>Drives the endpoints through the composed host against a real PostgreSQL.</summary>
/// <remarks>An in-memory server, so no port is bound. No broker runs either: the outbox keeps every publish inside the database.</remarks>
[Collection(nameof(PostgresCollection))]
[Trait("Category", "RequiresDocker")]
public sealed class TaskEndpointsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _app = await TestApiHost.StartAsync(Environments.Production, fixture.ConnectionString);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Creating_a_task_returns_201_with_a_location_and_the_created_task()
    {
        var response = await _client.PostAsJsonAsync(
            "/tasks",
            new { title = "  Renew passport  ", description = "Before the trip" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await ReadJsonAsync(response);
        created.GetProperty("title").GetString().ShouldBe("Renew passport");
        created.GetProperty("status").GetString().ShouldBe(nameof(TaskItemStatus.New));

        // The Location header has to lead somewhere, which is what CreatedAtRoute is for.
        response.Headers.Location.ShouldNotBeNull();

        var followed = await _client.GetAsync(response.Headers.Location);
        followed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(followed)).GetProperty("id").GetString().ShouldBe(Id(created).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task A_request_without_a_title_is_rejected_before_the_domain_sees_it(string? title)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title, description = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");

        ShouldReportFieldError(await ReadJsonAsync(response), nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task A_title_past_the_length_the_column_allows_is_rejected()
    {
        var response = await _client.PostAsJsonAsync(
            "/tasks",
            new { title = new string('a', TaskTitle.MaxLength + 1), description = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ShouldReportFieldError(await ReadJsonAsync(response), nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task A_status_read_from_a_response_can_be_sent_straight_back()
    {
        // Names in both directions. Bound as numbers, the spelling a client just read would not parse.
        var created = await CreateAsync("Round-trip the status");
        created.GetProperty("status").GetString().ShouldBe(nameof(TaskItemStatus.New));

        var response = await _client.PutAsJsonAsync(
            $"/tasks/{Id(created)}/status",
            new { status = nameof(TaskItemStatus.InProgress) });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("status").GetString()
            .ShouldBe(nameof(TaskItemStatus.InProgress));
    }

    [Fact]
    public async Task A_status_move_the_rules_forbid_returns_409()
    {
        var created = await CreateAsync("Archive then complete");

        var archived = await _client.PutAsJsonAsync(
            $"/tasks/{Id(created)}/status",
            new { status = nameof(TaskItemStatus.Archived) });

        archived.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await _client.PutAsJsonAsync(
            $"/tasks/{Id(created)}/status",
            new { status = nameof(TaskItemStatus.Completed) });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().ShouldBe("task.status.invalid-transition");

        // Title and type come from the framework's table for the status, not from this project.
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.GetProperty("type").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_status_outside_the_enum_returns_400_rather_than_409()
    {
        // A value nobody declared is a malformed request, not a state conflict - retry-on-409
        // clients would loop on the latter.
        var created = await CreateAsync("Undefined status");

        var response = await _client.PutAsJsonAsync($"/tasks/{Id(created)}/status", new { status = 99 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ShouldReportFieldError(await ReadJsonAsync(response), nameof(ChangeTaskStatusRequest.Status));
    }

    [Fact]
    public async Task Updating_a_task_returns_the_new_details()
    {
        var created = await CreateAsync("Renew passport");

        var response = await _client.PutAsJsonAsync(
            $"/tasks/{Id(created)}",
            new { title = "Renew passport urgently", description = "The office closes in July" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await ReadJsonAsync(response);
        updated.GetProperty("title").GetString().ShouldBe("Renew passport urgently");
        updated.GetProperty("description").GetString().ShouldBe("The office closes in July");
    }

    [Fact]
    public async Task Deleting_a_task_returns_204_and_the_task_is_then_gone()
    {
        var created = await CreateAsync("Cancel the subscription");

        var deleted = await _client.DeleteAsync($"/tasks/{Id(created)}");
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await _client.GetAsync($"/tasks/{Id(created)}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task An_unknown_task_returns_404_carrying_the_error_code(string method)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), $"/tasks/{Guid.NewGuid()}");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().ShouldBe("task.not-found");
        problem.GetProperty("title").GetString().ShouldBe("Not Found");
    }

    [Fact]
    public async Task A_body_that_cannot_be_read_returns_400_rather_than_500()
    {
        var response = await _client.PostAsync(
            "/tasks",
            new StringContent("{ not json", Encoding.UTF8, "application/json"));

        await ShouldBeAProblemAsync(response);
    }

    [Fact]
    public async Task A_status_the_query_binder_cannot_parse_still_answers_with_problem_details()
    {
        // The binder rejects this before any filter or handler runs. Left to the framework it would
        // be an empty 400 outside Development, which a client parsing the documented body chokes on.
        await ShouldBeAProblemAsync(await _client.GetAsync("/tasks?status=Bogus"));
    }

    [Fact]
    public async Task Listing_by_status_returns_only_that_status()
    {
        var created = await CreateAsync("Stays new");

        var response = await _client.GetAsync($"/tasks?status={nameof(TaskItemStatus.Archived)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = await ReadJsonAsync(response);
        var tasks = page.GetProperty("items").EnumerateArray().ToArray();

        tasks.ShouldAllBe(task => task.GetProperty("status").GetString() == nameof(TaskItemStatus.Archived));
        tasks.ShouldNotContain(task => task.GetProperty("id").GetString() == Id(created).ToString());

        // The envelope is what lets a client ask for the next page.
        page.GetProperty("number").GetInt32().ShouldBe(1);
        page.GetProperty("size").GetInt32().ShouldBe(20);
        page.GetProperty("totalItems").GetInt64().ShouldBeGreaterThanOrEqualTo(tasks.Length);
    }

    [Fact]
    public async Task A_page_size_past_the_ceiling_is_refused()
    {
        var response = await _client.GetAsync("/tasks?pageSize=1000");

        await ShouldBeAProblemAsync(response);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().ShouldBe("tasks.paging.invalid");
    }

    private static Guid Id(JsonElement task) => task.GetProperty("id").GetGuid();

    /// <summary>Asserts a 400 that actually carries a problem body, rather than an empty response.</summary>
    private static async Task ShouldBeAProblemAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // ShouldNotBeNull first: ContentType?.MediaType.ShouldBe(...) short-circuits away the whole
        // assertion when there is no content type, which is exactly the case worth catching.
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        (await response.ReadJsonAsync()).GetProperty("status").GetInt32().ShouldBe(400);
    }

    /// <summary>Asserts the body names the offending field and still carries the shared error code.</summary>
    private static void ShouldReportFieldError(JsonElement problem, string field)
    {
        problem.GetProperty("code").GetString().ShouldBe(ValidationFilter<object>.ErrorCode);
        problem.GetProperty("errors").GetProperty(field).EnumerateArray().ShouldNotBeEmpty();
    }

    private static Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) => response.ReadJsonAsync();

    private async Task<JsonElement> CreateAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title, description = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return await ReadJsonAsync(response);
    }
}

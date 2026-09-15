using FinBeat.TaskManagement.Api.Endpoints.Validation;
using FinBeat.TaskManagement.Application.Results;
using FinBeat.TaskManagement.Application.Tasks;
using FinBeat.TaskManagement.Application.Tasks.Commands;
using FinBeat.TaskManagement.Application.Tasks.Queries;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;

namespace FinBeat.TaskManagement.Api.Endpoints;

internal static class TaskEndpoints
{
    private const string GetTaskByIdRoute = "GetTaskById";

    /// <summary>Maps every task endpoint under <c>/tasks</c>.</summary>
    /// <remarks>Successful responses are read off the Results union; only failures are declared.</remarks>
    internal static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tasks = endpoints.MapGroup("/tasks")
            .WithTags("Tasks")
            .WithOpenApi(operation =>
            {
                // Group-wide, so the four routes that take an id describe it once between them.
                Describe(operation, "id", "The task id.");

                return operation;
            });

        tasks.MapPost("/", CreateAsync)
            .WithName("CreateTask")
            .WithSummary("Creates a task")
            .WithDescription("The task starts in the New status, and its created and modified timestamps are equal.")
            .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>()
            .ProducesValidationProblem();

        tasks.MapGet("/", ListAsync)
            .WithName("GetTasks")
            .WithSummary("Lists tasks, oldest first")
            .WithDescription(
                "One page at a time, ordered by creation and then by id so pages neither repeat a task "
                + "nor skip one. Pass a status to return only tasks in it; Archived tasks are included "
                + $"unless a status narrows them out. Page size is at most {GetTasksHandler.MaxPageSize}.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi(operation =>
            {
                // A parameter $refs the documented component, and Swagger UI does not show a $ref
                // target's description beside the field, so the parameter carries its own.
                Describe(operation, "status", "Return only tasks in this status. Omit for all of them.");
                Describe(operation, "page", "Which page to return, counting from one.");
                Describe(
                    operation,
                    "pageSize",
                    $"How many tasks to return, from 1 to {GetTasksHandler.MaxPageSize}.");

                return operation;
            });

        tasks.MapGet("/{id:guid}", GetAsync)
            .WithName(GetTaskByIdRoute)
            .WithSummary("Reads one task")
            .ProducesProblem(StatusCodes.Status404NotFound);

        tasks.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTaskDetails")
            .WithSummary("Replaces a task's title and description")
            .WithDescription("Submitting the values the task already holds changes nothing and raises no event, so the history stays honest.")
            .AddEndpointFilter<ValidationFilter<UpdateTaskDetailsRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        tasks.MapPut("/{id:guid}/status", ChangeStatusAsync)
            .WithName("ChangeTaskStatus")
            .WithSummary("Moves a task to another status")
            .WithDescription(
                $"Allowed moves: {DescribeTransitions()}. Anything else is a conflict. Setting the status a "
                + "task already holds changes nothing and raises no event.")
            .AddEndpointFilter<ValidationFilter<ChangeTaskStatusRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        tasks.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTask")
            .WithSummary("Deletes a task")
            .WithDescription("The row is removed for good. Move the task to Archived to keep its history instead.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<CreatedAtRoute<TaskResponse>, ProblemHttpResult>> CreateAsync(
        CreateTaskRequest request,
        CreateTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateTaskCommand(request.Title, request.Description),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, GetTaskByIdRoute, new { id = result.Value.Id })
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<Page<TaskResponse>>, ProblemHttpResult>> ListAsync(
        TaskItemStatus? status,
        GetTasksHandler handler,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = GetTasksHandler.DefaultPageSize)
    {
        var result = await handler.HandleAsync(new GetTasksQuery(status, page, pageSize), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<TaskResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        GetTaskByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTaskByIdQuery(id), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<TaskResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateTaskDetailsRequest request,
        UpdateTaskDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateTaskDetailsCommand(id, request.Title, request.Description),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<TaskResponse>, ProblemHttpResult>> ChangeStatusAsync(
        Guid id,
        ChangeTaskStatusRequest request,
        ChangeTaskStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeTaskStatusCommand(id, request.Status),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        DeleteTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteTaskCommand(id), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    // Reads the transition matrix off TaskItemStatusRules instead of restating it in prose, so the
    // published description cannot drift from rules the code has since changed.
    private static string DescribeTransitions()
    {
        var statuses = Enum.GetValues<TaskItemStatus>();

        var moves = statuses
            .Select(from => (From: from, To: statuses.Where(to => TaskItemStatusRules.CanTransition(from, to))))
            .Where(move => move.To.Any())
            .Select(move => $"{move.From} to {JoinWithOr(move.To)}");

        return string.Join("; ", moves);
    }

    // Joins the destinations of one move the way the sentence reads them: "A, B or C".
    private static string JoinWithOr(IEnumerable<TaskItemStatus> destinations)
    {
        var names = destinations.Select(status => status.ToString()).ToArray();

        return names.Length == 1
            ? names[0]
            : string.Join(", ", names[..^1]) + " or " + names[^1];
    }

    // Documents one parameter of an operation, if it has one by that name.
    private static void Describe(OpenApiOperation operation, string parameterName, string description)
    {
        var parameter = operation.Parameters
            .FirstOrDefault(candidate => string.Equals(candidate.Name, parameterName, StringComparison.Ordinal));

        if (parameter is not null)
        {
            parameter.Description = description;
        }
    }
}

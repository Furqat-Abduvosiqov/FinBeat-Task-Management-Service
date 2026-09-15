using FinBeat.TaskManagement.Contracts.Tasks;
using MassTransit;

namespace FinBeat.TaskManagement.Listener.Consumers;

/// <summary>Logs tasks as they are created.</summary>
/// <param name="logger">Where the event is recorded.</param>
internal sealed class TaskCreatedConsumer(ILogger<TaskCreatedConsumer> logger) : IConsumer<TaskCreated>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TaskCreated> context)
    {
        logger.LogInformation(
            "Task {TaskId} was created at {OccurredOnUtc:O} as {Status}: {Title}",
            context.Message.TaskId,
            context.Message.OccurredOnUtc,
            context.Message.Status,
            context.Message.Title);

        return Task.CompletedTask;
    }
}

/// <summary>Logs changes to a task's title and description.</summary>
/// <param name="logger">Where the event is recorded.</param>
internal sealed class TaskDetailsUpdatedConsumer(ILogger<TaskDetailsUpdatedConsumer> logger)
    : IConsumer<TaskDetailsUpdated>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TaskDetailsUpdated> context)
    {
        logger.LogInformation(
            "Task {TaskId} was retitled at {OccurredOnUtc:O}: {Title}",
            context.Message.TaskId,
            context.Message.OccurredOnUtc,
            context.Message.Title);

        return Task.CompletedTask;
    }
}

/// <summary>Logs a task moving from one status to another.</summary>
/// <param name="logger">Where the event is recorded.</param>
internal sealed class TaskStatusChangedConsumer(ILogger<TaskStatusChangedConsumer> logger)
    : IConsumer<TaskStatusChanged>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TaskStatusChanged> context)
    {
        logger.LogInformation(
            "Task {TaskId} moved from {PreviousStatus} to {CurrentStatus} at {OccurredOnUtc:O}",
            context.Message.TaskId,
            context.Message.PreviousStatus,
            context.Message.CurrentStatus,
            context.Message.OccurredOnUtc);

        return Task.CompletedTask;
    }
}

/// <summary>Logs tasks as they are deleted.</summary>
/// <param name="logger">Where the event is recorded.</param>
internal sealed class TaskDeletedConsumer(ILogger<TaskDeletedConsumer> logger) : IConsumer<TaskDeleted>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TaskDeleted> context)
    {
        logger.LogInformation(
            "Task {TaskId} was deleted at {OccurredOnUtc:O}",
            context.Message.TaskId,
            context.Message.OccurredOnUtc);

        return Task.CompletedTask;
    }
}

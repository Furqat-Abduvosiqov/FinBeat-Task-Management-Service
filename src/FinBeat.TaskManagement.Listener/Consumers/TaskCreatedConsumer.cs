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
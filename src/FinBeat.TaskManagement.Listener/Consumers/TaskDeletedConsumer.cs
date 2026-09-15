using FinBeat.TaskManagement.Contracts.Tasks;
using MassTransit;

namespace FinBeat.TaskManagement.Listener.Consumers;

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
using FinBeat.TaskManagement.Contracts.Tasks;
using MassTransit;

namespace FinBeat.TaskManagement.Listener.Consumers;

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
using FinBeat.TaskManagement.Contracts.Tasks;
using MassTransit;

namespace FinBeat.TaskManagement.Listener.Consumers;

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

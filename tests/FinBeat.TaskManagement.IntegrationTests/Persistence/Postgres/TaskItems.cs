using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

/// <summary>Ready-made <see cref="TaskItem"/> instances for the Postgres persistence tests.</summary>
/// <remarks>"Renew passport" is the default title because no test using it asserts on the title text; one that does passes it explicitly.</remarks>
internal static class TaskItems
{
    /// <summary>A task titled "Renew passport" with no description.</summary>
    public static TaskItem RenewPassport(TimeProvider clock) =>
        TaskItem.Create(TaskTitle.Create("Renew passport"), TaskDescription.None, clock);

    /// <summary>A task titled "Renew passport" with the given description.</summary>
    public static TaskItem RenewPassport(TaskDescription description, TimeProvider clock) =>
        TaskItem.Create(TaskTitle.Create("Renew passport"), description, clock);

    /// <summary>A task with the given title and no description.</summary>
    public static TaskItem WithTitle(string title, TimeProvider clock) =>
        TaskItem.Create(TaskTitle.Create(title), TaskDescription.None, clock);
}

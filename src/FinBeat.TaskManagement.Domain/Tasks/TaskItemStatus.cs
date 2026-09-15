namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>Where a task is in its lifecycle.</summary>
/// <remarks>Archived is a fourth status, added because deletion here is real.</remarks>
public enum TaskItemStatus
{
    /// <summary>Created, not started.</summary>
    New = 1,

    /// <summary>Being worked on.</summary>
    InProgress = 2,

    /// <summary>Finished.</summary>
    Completed = 3,

    /// <summary>Kept for its history rather than deleted.</summary>
    Archived = 4,
}

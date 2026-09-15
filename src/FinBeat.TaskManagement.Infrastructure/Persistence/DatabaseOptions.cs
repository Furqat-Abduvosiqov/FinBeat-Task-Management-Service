using System.ComponentModel.DataAnnotations;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The database settings, bound from the <c>ConnectionStrings</c> configuration section.</summary>
/// <remarks>Validated at host start, so a missing connection string is named at startup rather than surfacing as a null reference inside Npgsql on whichever request runs the first query.</remarks>
public sealed class DatabaseOptions
{
    /// <summary>The configuration section these settings bind from.</summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>The name of the connection string within <see cref="SectionName"/>.</summary>
    public const string ConnectionStringName = nameof(TaskManagement);

    /// <summary>The connection string for the task management database.</summary>
    [Required(
        AllowEmptyStrings = false,
        ErrorMessage = "Set 'ConnectionStrings:TaskManagement', or the 'ConnectionStrings__TaskManagement' environment variable.")]
    public string TaskManagement { get; init; } = string.Empty;
}

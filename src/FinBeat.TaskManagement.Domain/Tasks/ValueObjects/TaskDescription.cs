using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

/// <summary>A task's optional description, at most <see cref="MaxLength"/> characters.</summary>
/// <remarks>
/// Absence is <see cref="None"/>, not null, so the aggregate's <c>Description</c> never needs a
/// null check. Infrastructure can still map it to a nullable column.
/// </remarks>
public sealed record TaskDescription
{
    /// <summary>The longest a description may be, after trimming.</summary>
    public const int MaxLength = 2000;

    /// <summary>No description.</summary>
    public static readonly TaskDescription None = new(string.Empty);

    private TaskDescription(string value) => Value = value;

    /// <summary>The trimmed description, empty when there is none.</summary>
    public string Value { get; }

    /// <summary>Trims and validates a description. Null or whitespace gives <see cref="None"/>.</summary>
    /// <exception cref="InvalidTaskDescriptionException">Longer than <see cref="MaxLength"/> once trimmed.</exception>
    public static TaskDescription Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return None;
        }

        var trimmed = value.Trim();

        return trimmed.Length > MaxLength
            ? throw new InvalidTaskDescriptionException($"A task description cannot exceed {MaxLength} characters.")
            : new TaskDescription(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

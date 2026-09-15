using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Domain.Tasks.ValueObjects;

/// <summary>A task's title: required, trimmed, at most <see cref="MaxLength"/> characters.</summary>
/// <remarks>Private constructor and a get-only property, so <see cref="Create"/> is the only way in.</remarks>
public sealed record TaskTitle
{
    /// <summary>The longest a title may be, after trimming.</summary>
    public const int MaxLength = 200;

    private TaskTitle(string value) => Value = value;

    /// <summary>The trimmed title.</summary>
    public string Value { get; }

    /// <summary>Trims and validates a title.</summary>
    /// <exception cref="InvalidTaskTitleException">Empty once trimmed, or longer than <see cref="MaxLength"/>.</exception>
    public static TaskTitle Create(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new InvalidTaskTitleException("A task title cannot be empty or whitespace-only.");
        }

        return trimmed.Length > MaxLength 
            ? throw new InvalidTaskTitleException($"A task title cannot exceed {MaxLength} characters.") 
            : new TaskTitle(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

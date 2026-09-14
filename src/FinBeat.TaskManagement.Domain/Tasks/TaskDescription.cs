using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// A task's optional description, bounded to <see cref="MaxLength"/> characters.
/// </summary>
/// <remarks>
/// Absence is represented by <see cref="None"/> — an instance wrapping <see cref="string.Empty"/> —
/// rather than by a null reference. That keeps <c>Description</c> non-nullable on the
/// <c>TaskItem</c> aggregate, so callers never need a null check, while an Infrastructure-layer
/// mapping can still translate <see cref="None"/> to a nullable database column if that is the
/// preferred storage shape.
/// </remarks>
public sealed record TaskDescription
{
    /// <summary>The longest a description may be, in characters, after trimming.</summary>
    public const int MaxLength = 2000;

    /// <summary>The absence of a description, represented as an empty value rather than null.</summary>
    public static readonly TaskDescription None = new(string.Empty);

    private TaskDescription(string value) => Value = value;

    /// <summary>The trimmed, validated description text. Empty when there is no description.</summary>
    public string Value { get; }

    /// <summary><see langword="true"/> when this instance carries no description text.</summary>
    public bool IsEmpty => Value.Length == 0;

    /// <summary>Validates and wraps <paramref name="value"/> as a <see cref="TaskDescription"/>.</summary>
    /// <param name="value">
    /// The raw description text. <see langword="null"/> or whitespace-only input produces
    /// <see cref="None"/>; any other value is trimmed before validation.
    /// </param>
    /// <returns>A <see cref="TaskDescription"/> wrapping the trimmed value, or <see cref="None"/>.</returns>
    /// <exception cref="InvalidTaskDescriptionException">
    /// <paramref name="value"/>, once trimmed, is longer than <see cref="MaxLength"/> characters.
    /// </exception>
    public static TaskDescription Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return None;
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidTaskDescriptionException($"A task description cannot exceed {MaxLength} characters.");
        }

        return new TaskDescription(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

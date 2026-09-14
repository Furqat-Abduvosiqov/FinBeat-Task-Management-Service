using FinBeat.TaskManagement.Domain.Tasks.Exceptions;

namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// A task's title: required, trimmed of surrounding whitespace, and bounded to
/// <see cref="MaxLength"/> characters.
/// </summary>
/// <remarks>
/// The constructor is private and <see cref="Value"/> has no setter, so <see cref="Create"/> is the
/// only way to obtain an instance. That closes off the one loophole records otherwise offer: a
/// caller cannot write <c>title with { Value = "" }</c> to produce an unvalidated title, because
/// there is no accessible member to target in the <c>with</c> expression.
/// </remarks>
public sealed record TaskTitle
{
    /// <summary>The longest a title may be, in characters, after trimming.</summary>
    public const int MaxLength = 200;

    private TaskTitle(string value) => Value = value;

    /// <summary>The trimmed, validated title text.</summary>
    public string Value { get; }

    /// <summary>Validates and wraps <paramref name="value"/> as a <see cref="TaskTitle"/>.</summary>
    /// <param name="value">The raw title text. Leading and trailing whitespace is trimmed before validation.</param>
    /// <returns>A <see cref="TaskTitle"/> wrapping the trimmed value.</returns>
    /// <exception cref="InvalidTaskTitleException">
    /// <paramref name="value"/> is <see langword="null"/>, empty, or whitespace-only once trimmed,
    /// or the trimmed value is longer than <see cref="MaxLength"/> characters.
    /// </exception>
    public static TaskTitle Create(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new InvalidTaskTitleException("A task title cannot be empty or whitespace-only.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidTaskTitleException($"A task title cannot exceed {MaxLength} characters.");
        }

        return new TaskTitle(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

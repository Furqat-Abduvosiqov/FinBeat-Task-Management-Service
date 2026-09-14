namespace FinBeat.TaskManagement.Domain.Tasks;

/// <summary>
/// Strongly-typed identifier for a <c>TaskItem</c>.
/// </summary>
/// <param name="Value">The underlying identifier value.</param>
/// <remarks>
/// <para>
/// <b>Why the id is generated here rather than by the database.</b> A task has an identity from the
/// instant it is constructed, not from the instant it is saved. <c>Entity&lt;TId&gt;</c> compares
/// entities by their id, so if ids were assigned by a database sequence, every unsaved task would
/// carry <see cref="Guid.Empty"/> until it was persisted — two <em>different</em> freshly created
/// tasks would compare equal in the meantime, and putting both in a <see cref="HashSet{T}"/> would
/// silently keep only one. Assigning the id in <see cref="New"/>, before the aggregate is ever
/// handed to a repository, removes that whole failure mode.
/// </para>
/// <para>
/// <b>The trade-off.</b> <see cref="Guid.NewGuid"/> is random, so consecutive inserts scatter across
/// the primary key index rather than clustering at the end of it. A time-ordered UUID (version 7)
/// would avoid that, and PostgreSQL compares <c>uuid</c> byte-wise so it would genuinely cluster
/// there — though not on SQL Server, whose <c>uniqueidentifier</c> collation sorts the last six
/// bytes first and defeats the layout. It is not worth the code here: the effect shows up at
/// millions of rows under sustained insert load, and <c>Guid.CreateVersion7</c> only exists from
/// .NET 9 while this project targets .NET 8, so it would mean hand-rolling RFC 9562. Revisit if the
/// insert volume ever justifies it.
/// </para>
/// </remarks>
public readonly record struct TaskItemId(Guid Value)
{
    /// <summary>Generates a new unique identifier.</summary>
    /// <returns>A new, unique <see cref="TaskItemId"/>.</returns>
    public static TaskItemId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing <see cref="Guid"/> value as a <see cref="TaskItemId"/>, for example when rehydrating an id from storage.</summary>
    /// <param name="value">The underlying value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <returns>A <see cref="TaskItemId"/> wrapping <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static TaskItemId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("A task id cannot be an empty guid.", nameof(value))
            : new TaskItemId(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

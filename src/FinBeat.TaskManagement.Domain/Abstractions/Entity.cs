namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>
/// Base class for entities identified by a strongly-typed id of type <typeparamref name="TId"/>.
/// </summary>
/// <remarks>
/// Entities are compared by identity, not by structural equality: two instances with the same
/// property values but different ids are different entities, while two instances with the same id
/// are the same entity even if their in-memory state has diverged. Equality additionally requires
/// the same runtime type, because <typeparamref name="TId"/> alone does not disambiguate between
/// unrelated entities that happen to share an id type and, by coincidence, a value — a
/// <c>TaskItem</c> and a hypothetical <c>Note</c> built on the same <see cref="Guid"/>-based id
/// must never compare equal just because their <see cref="Id"/> values match.
/// </remarks>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>Initializes a new entity with the given identity.</summary>
    /// <param name="id">The entity's identifier, assigned once and never changed afterwards.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>
    /// Parameterless constructor reserved for EF Core materialization: EF Core populates
    /// <see cref="Id"/> and other state via reflection when rehydrating an entity from the
    /// database, bypassing application constructors entirely.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>The entity's identifier. Immutable after construction.</summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>Determines whether two entities are equal by runtime type and id.</summary>
    /// <param name="left">The first entity, or <see langword="null"/>.</param>
    /// <param name="right">The second entity, or <see langword="null"/>.</param>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two entities are not equal by runtime type and id.</summary>
    /// <param name="left">The first entity, or <see langword="null"/>.</param>
    /// <param name="right">The second entity, or <see langword="null"/>.</param>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

    /// <summary>Determines whether <paramref name="other"/> is the same runtime type as this instance and has an equal <see cref="Id"/>.</summary>
    /// <param name="other">The entity to compare against.</param>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

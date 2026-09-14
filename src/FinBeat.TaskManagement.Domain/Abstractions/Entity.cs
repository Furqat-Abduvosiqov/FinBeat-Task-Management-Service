namespace FinBeat.TaskManagement.Domain.Abstractions;

/// <summary>Base class for entities, which are compared by identity rather than by value.</summary>
/// <remarks>
/// Equality requires the same runtime type as well as the same id, so a task and some other entity
/// that happen to share a <see cref="Guid"/> are never mistaken for each other.
/// </remarks>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>Creates an entity with the given identity.</summary>
    protected Entity(TId id) => Id = id;

    /// <summary>Reserved for EF Core, which sets the properties by reflection afterwards.</summary>
    protected Entity()
    {
    }

    /// <summary>The entity's identifier. Set once, never changed.</summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>Compares two entities by runtime type and id.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Compares two entities by runtime type and id.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

    /// <summary>Compares by runtime type and id.</summary>
    /// <remarks>
    /// An entity with a default id has no identity yet — where EF Core leaves one between
    /// constructing and populating it. Those fall back to reference equality, so two unsaved
    /// entities are not mistaken for the same one.
    /// </remarks>
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

        if (GetType() != other.GetType())
        {
            return false;
        }

        // EqualityComparer rather than Id.Equals: TId is notnull, but that still allows reference
        // types, whose Id is null after the EF constructor. It also avoids boxing for structs.
        var comparer = EqualityComparer<TId>.Default;

        return !comparer.Equals(Id, default!)
            && !comparer.Equals(other.Id, default!)
            && comparer.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Whether an id has been assigned yet. False only while EF Core is materializing.</summary>
    protected bool HasIdentity => !EqualityComparer<TId>.Default.Equals(Id, default!);
}

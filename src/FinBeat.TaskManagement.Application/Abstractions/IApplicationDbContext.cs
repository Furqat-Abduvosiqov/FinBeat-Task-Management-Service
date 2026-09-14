using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Abstractions;

/// <summary>The database, as the use cases see it.</summary>
/// <remarks>
/// <para>
/// There is no repository here on purpose. <c>DbContext</c> is already a unit of work and
/// <c>DbSet</c> is already a repository, so wrapping them adds a second abstraction over the same
/// thing — and one that ends up a worse query language than the LINQ it hides, as soon as filtering
/// and paging arrive.
/// </para>
/// <para>
/// What this interface does buy is that Application states what it needs from storage and
/// Infrastructure supplies it, so the dependency still points inward. The cost is that Application
/// now references EF Core for <c>DbSet</c>; the architecture rules allow exactly that package and
/// nothing else.
/// </para>
/// </remarks>
public interface IApplicationDbContext
{
    /// <summary>The tasks.</summary>
    DbSet<TaskItem> Tasks { get; }

    /// <summary>Commits everything tracked since the last save.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

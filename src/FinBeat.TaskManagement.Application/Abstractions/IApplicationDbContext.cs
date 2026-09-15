using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Application.Abstractions;

/// <summary>The database, as the use cases see it.</summary>
/// <remarks>No repository wrapper: DbContext is already a unit of work and DbSet already a repository.</remarks>
public interface IApplicationDbContext
{
    /// <summary>The tasks.</summary>
    DbSet<TaskItem> Tasks { get; }

    /// <summary>Commits everything tracked since the last save.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

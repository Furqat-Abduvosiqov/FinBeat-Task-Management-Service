using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The EF Core <see cref="DbContext"/> this application persists through.</summary>
/// <remarks>
/// <para>
/// No <c>SaveChangesAsync</c> override appears here. <see cref="DbContext"/> already declares
/// <c>Task&lt;int&gt; SaveChangesAsync(CancellationToken)</c>, which satisfies
/// <see cref="IApplicationDbContext"/> implicitly — there is nothing to add yet.
/// </para>
/// <para>
/// This is also where a future domain-event dispatch belongs, and it will have to read the change
/// tracker <em>before</em> calling into the base save, not after: EF detaches a deleted entity once
/// the save completes, and a detached entity has taken its events with it by the time anything could
/// read them back.
/// </para>
/// </remarks>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">The context options. Only the provider and connection string are needed.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>The tasks.</summary>
    /// <remarks>The property name is load-bearing: with no explicit <c>ToTable</c> call, it is what names the table <c>tasks</c>.</remarks>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <inheritdoc />
    /// <remarks>
    /// The naming convention lives here rather than at each place that builds options, so that no caller
    /// can leave it out. Runtime DI, the design-time factory and the test fixtures all construct this
    /// context independently, and a path that forgot it would migrate a PascalCase schema and then query
    /// a snake_case one — which compiles cleanly and surfaces much later as
    /// <c>relation "tasks" does not exist</c>.
    /// </remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.UseSnakeCaseNamingConvention();

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}

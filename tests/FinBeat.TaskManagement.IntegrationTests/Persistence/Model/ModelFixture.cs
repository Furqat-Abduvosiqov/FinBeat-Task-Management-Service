using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

/// <summary>
/// Builds the EF Core model exactly the way the application builds it: through
/// <see cref="DependencyInjection.AddInfrastructure"/>, never a hand-rolled
/// <see cref="Microsoft.EntityFrameworkCore.DbContextOptionsBuilder"/>. A test that called
/// <c>UseSnakeCaseNamingConvention</c> itself would only prove that the test produces snake_case, not
/// that the application does.
/// </summary>
/// <remarks>
/// <see cref="ConnectionString"/> parses but is never dialled: building a model does not open a
/// connection, so there is no need for a real database here. See the Postgres fixture for the tests
/// that actually talk to PostgreSQL.
/// </remarks>
public sealed class ModelFixture : IDisposable
{
    /// <summary>A syntactically valid connection string that this fixture never connects to.</summary>
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=model_shape_only;Username=none;Password=none";

    /// <summary>Builds the DI container and, from it, the model.</summary>
    public ModelFixture()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfiguration(ConnectionString));
        Provider = services.BuildServiceProvider();

        using var scope = Provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Model = context.Model;
        DesignTimeModel = context.GetService<IDesignTimeModel>().Model;
        TaskEntityType = Model.FindEntityType(typeof(TaskItem))
            ?? throw new InvalidOperationException($"{nameof(TaskItem)} is not part of the model.");
        TaskTable = StoreObjectIdentifier.Create(TaskEntityType, StoreObjectType.Table)
            ?? throw new InvalidOperationException($"{nameof(TaskItem)} is not mapped to a table.");
    }

    /// <summary>The container <see cref="DependencyInjection.AddInfrastructure"/> populated.</summary>
    public ServiceProvider Provider { get; }

    /// <summary>The runtime EF Core model, built through the same path the application uses.</summary>
    public IModel Model { get; }

    /// <summary>The design-time model the same context produces.</summary>
    /// <remarks>
    /// Not the same object as <see cref="Model"/>: EF strips design-time-only annotations from the
    /// runtime model. This is the model migrations are scaffolded from, so it is the one to compare
    /// against when asking whether what gets migrated matches what gets queried.
    /// </remarks>
    public IModel DesignTimeModel { get; }

    /// <summary><see cref="TaskItem"/>'s entity type within <see cref="Model"/>.</summary>
    public IEntityType TaskEntityType { get; }

    /// <summary>The store identifier for the table <see cref="TaskEntityType"/> maps to.</summary>
    public StoreObjectIdentifier TaskTable { get; }

    /// <summary>Looks up a property of <see cref="TaskEntityType"/> by its CLR name.</summary>
    /// <param name="propertyName">The property name, typically <c>nameof(TaskItem.X)</c>.</param>
    /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> is not mapped.</exception>
    public IProperty GetRequiredProperty(string propertyName) =>
        TaskEntityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException(
                $"{propertyName} is not a mapped property of {nameof(TaskItem)}.");

    /// <summary>Builds configuration carrying a single connection string, the way the Api host does.</summary>
    public static IConfiguration BuildConfiguration(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:" + DependencyInjection.ConnectionStringName] = connectionString
            })
            .Build();

    /// <summary>
    /// Builds an <see cref="ApplicationDbContext"/> the way <c>dotnet ef</c> does — from a bare
    /// connection string rather than through DI. The caller owns disposal.
    /// </summary>
    /// <remarks>
    /// Note what is deliberately absent: no naming convention is applied here. The context applies it
    /// itself in <c>OnConfiguring</c>, which is the property the parity test exists to confirm — a
    /// context built this crudely still has to produce the model the application queries.
    /// </remarks>
    public static ApplicationDbContext CreateDesignTimeContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseNpgsql(ConnectionString);

        return new ApplicationDbContext(options.Options);
    }

    /// <inheritdoc />
    public void Dispose() => Provider.Dispose();
}

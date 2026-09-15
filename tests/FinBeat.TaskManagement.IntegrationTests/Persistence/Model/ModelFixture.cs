using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

/// <summary>Builds the EF Core model the way the application does: through <see cref="DependencyInjection.AddInfrastructure"/>, never a hand-rolled <see cref="Microsoft.EntityFrameworkCore.DbContextOptionsBuilder"/>.</summary>
/// <remarks><see cref="ConnectionString"/> parses but is never dialled: building a model needs no real database.</remarks>
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
    /// <remarks>Not the same object as <see cref="Model"/>: EF strips design-time-only annotations from the runtime model.</remarks>
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

    /// <summary>Builds an <see cref="ApplicationDbContext"/> the way <c>dotnet ef</c> does — from a bare connection string rather than through DI. The caller owns disposal.</summary>
    /// <remarks>No naming convention applied here on purpose: <c>OnConfiguring</c> applies it, which is exactly what the parity test confirms.</remarks>
    public static ApplicationDbContext CreateDesignTimeContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseNpgsql(ConnectionString);

        return new ApplicationDbContext(options.Options);
    }

    /// <inheritdoc />
    public void Dispose() => Provider.Dispose();
}

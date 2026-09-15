using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
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
    /// Not the same object as <see cref="Model"/>, and the difference matters here. EF strips
    /// design-time-only annotations from the runtime model, and the <c>Npgsql:Enum:</c> annotation
    /// <c>HasPostgresEnum</c> writes is one of them — so <c>GetPostgresEnums()</c> on
    /// <see cref="Model"/> returns nothing at all. Anything asserting on the native enum has to ask
    /// for this model, which is also the one migrations are scaffolded from.
    /// </remarks>
    public IModel DesignTimeModel { get; }

    /// <summary><see cref="TaskItem"/>'s entity type within <see cref="Model"/>.</summary>
    public IEntityType TaskEntityType { get; }

    /// <summary>The store identifier for the table <see cref="TaskEntityType"/> maps to.</summary>
    public StoreObjectIdentifier TaskTable { get; }

    /// <summary>Builds configuration carrying a single connection string, the way the Api host does.</summary>
    public static IConfiguration BuildConfiguration(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:" + DependencyInjection.ConnectionStringName] = connectionString
            })
            .Build();

    /// <inheritdoc />
    public void Dispose() => Provider.Dispose();
}

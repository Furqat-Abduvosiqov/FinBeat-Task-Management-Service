namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

/// <summary>
/// Groups every <see cref="ModelFixture"/> consumer onto one shared instance, so the DI container and
/// the EF Core model it builds are built exactly once per test run instead of once per test class.
/// </summary>
[CollectionDefinition(nameof(ModelCollection))]
public sealed class ModelCollection : ICollectionFixture<ModelFixture>
{
}

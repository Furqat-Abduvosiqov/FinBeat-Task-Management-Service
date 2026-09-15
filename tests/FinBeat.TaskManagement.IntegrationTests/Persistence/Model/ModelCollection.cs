namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

/// <summary>Groups every <see cref="ModelFixture"/> consumer so the model is built once per test run, not once per class.</summary>
[CollectionDefinition(nameof(ModelCollection))]
public sealed class ModelCollection : ICollectionFixture<ModelFixture>
{
}

using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

/// <summary>Proves the context a host resolves retries the errors a restarted server raises.</summary>
/// <remarks>Opens no connection, so this needs neither Docker nor the fixture.</remarks>
public sealed class RetryStrategyTests
{
    [Fact]
    public void The_resolved_context_retries_transient_failures()
    {
        // Through AddInfrastructure, not a hand-built options object - the claim is about what a host
        // actually resolves.
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConfiguration.ForDatabase(TestConfiguration.UnusedConnectionString));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // False for the non-retrying default strategy, which once let a container restart surface as
        // a 500 - deleting EnableRetryOnFailure from OnConfiguring turns this red again.
        context.Database.CreateExecutionStrategy().RetriesOnFailure.ShouldBeTrue();
    }
}

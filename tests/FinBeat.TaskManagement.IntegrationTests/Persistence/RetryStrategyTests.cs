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
        // Through AddInfrastructure rather than a hand-built options object: the claim is about the
        // context a host resolves, and options assembled here by hand would prove nothing about the
        // ones DependencyInjection actually builds.
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConfiguration.ForDatabase(TestConfiguration.UnusedConnectionString));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // RetriesOnFailure rather than the concrete type, which lives in an Internal namespace.
        // It is false for NpgsqlExecutionStrategy - the non-retrying default the stack trace named
        // when a container restart surfaced 57P01 as a 500. Deleting EnableRetryOnFailure from
        // ApplicationDbContext.OnConfiguring puts that strategy back here and turns this red.
        context.Database.CreateExecutionStrategy().RetriesOnFailure.ShouldBeTrue();
    }
}

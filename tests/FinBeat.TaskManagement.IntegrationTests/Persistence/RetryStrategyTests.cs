using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

/// <summary>Proves the context a host resolves retries the errors a restarted server raises.</summary>
/// <remarks>
/// Reaches no database: building the options and asking for the execution strategy opens no
/// connection, so this needs neither Docker nor the fixture.
/// </remarks>
public sealed class RetryStrategyTests
{
    [Fact]
    public void The_resolved_context_retries_transient_failures()
    {
        // Through AddInfrastructure rather than a hand-built options object, because the defect this
        // guards against is a second UseNpgsql discarding what an earlier one configured - and only
        // the real registration path has two of them.
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConfiguration.ForDatabase(TestConfiguration.UnusedConnectionString));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // RetriesOnFailure rather than the concrete type, which lives in an Internal namespace.
        // It is false for NpgsqlExecutionStrategy - the non-retrying default the stack trace named
        // when a container restart surfaced 57P01 as a 500. Deleting EnableRetryOnFailure from
        // ApplicationDbContext.OnConfiguring, or moving it to a call a later UseNpgsql overwrites,
        // puts that strategy back here and turns this red.
        context.Database.CreateExecutionStrategy().RetriesOnFailure.ShouldBeTrue();
    }
}

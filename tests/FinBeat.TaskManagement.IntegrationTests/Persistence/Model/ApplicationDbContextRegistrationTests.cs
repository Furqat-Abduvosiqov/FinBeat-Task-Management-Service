using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class ApplicationDbContextRegistrationTests(ModelFixture fixture)
{
    [Fact]
    public void IApplicationDbContext_and_ApplicationDbContext_resolve_to_the_same_instance_within_one_scope()
    {
        // Two separate registrations would mean two change trackers, so SaveChangesAsync could
        // report success while saving nothing the use case actually changed.
        using var scope = fixture.Provider.CreateScope();

        IApplicationDbContext viaInterface = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        ApplicationDbContext viaConcreteType = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        viaInterface.ShouldBeSameAs(viaConcreteType);
    }

    [Fact]
    public void AddInfrastructure_with_no_connection_string_configured_throws()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.AddInfrastructure(emptyConfiguration));
    }
}

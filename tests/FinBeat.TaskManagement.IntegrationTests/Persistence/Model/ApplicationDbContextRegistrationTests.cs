using FinBeat.TaskManagement.Application.Abstractions;
using FinBeat.TaskManagement.Infrastructure;
using FinBeat.TaskManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Startup_validation_rejects_a_missing_connection_string(string? connectionString)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(ModelFixture.BuildConfiguration(connectionString));

        using var provider = services.BuildServiceProvider();

        // The host calls this during Build(), so it is the failure a deployment would actually see.
        var validator = provider.GetRequiredService<IStartupValidator>();

        Should.Throw<OptionsValidationException>(validator.Validate)
            .Message.ShouldContain($"{DatabaseOptions.SectionName}:{DatabaseOptions.ConnectionStringName}");
    }

    [Fact]
    public void Startup_validation_passes_when_a_connection_string_is_configured()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(ModelFixture.BuildConfiguration(ModelFixture.ConnectionString));

        using var provider = services.BuildServiceProvider();

        Should.NotThrow(provider.GetRequiredService<IStartupValidator>().Validate);
    }
}

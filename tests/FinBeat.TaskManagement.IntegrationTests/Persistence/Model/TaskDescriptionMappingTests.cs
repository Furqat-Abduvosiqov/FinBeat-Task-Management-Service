using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskDescriptionMappingTests(ModelFixture fixture)
{
    [Fact]
    public void Description_is_mapped_not_nullable_even_though_it_is_optional_in_the_domain()
    {
        var property = fixture.GetRequiredProperty(nameof(TaskItem.Description));

        // TaskDescription.None - an empty string, not null - is how the domain spells "no
        // description". A nullable column is the mistake that hands back a null Description on load.
        property.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(TaskDescription.MaxLength);

        // Exercised in both directions rather than asserted by runtime type name: HasConversion(lambda,
        // lambda) compiles to a compiler-generated ValueConverter<T,U> whose GetType().Name carries no
        // meaning.
        var converter = property.GetValueConverter().ShouldNotBeNull();
        converter.ProviderClrType.ShouldBe(typeof(string));
        converter.ConvertToProvider(TaskDescription.Create("Bring passport photos")).ShouldBe("Bring passport photos");
        converter.ConvertFromProvider("Bring passport photos").ShouldBeOfType<TaskDescription>().Value.ShouldBe("Bring passport photos");

        // Create returns the None singleton for an empty string, so a row with no description
        // deserialises to the exact instance the domain treats as "absent", not merely an equal one.
        converter.ConvertFromProvider("").ShouldBeSameAs(TaskDescription.None);
    }
}

using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskDescriptionMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskDescriptionMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void Description_is_mapped_not_nullable_even_though_it_is_optional_in_the_domain()
    {
        var property = _fixture.TaskEntityType.FindProperty(nameof(TaskItem.Description)).ShouldNotBeNull();

        // TaskDescription.None - an empty string, not null - is how the domain spells "no
        // description". A nullable column is the mistake that hands back a null Description on load.
        property.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(TaskDescription.MaxLength);
        property.GetValueConverter().ShouldNotBeNull().GetType().Name.ShouldBe("TaskDescriptionConverter");
    }
}

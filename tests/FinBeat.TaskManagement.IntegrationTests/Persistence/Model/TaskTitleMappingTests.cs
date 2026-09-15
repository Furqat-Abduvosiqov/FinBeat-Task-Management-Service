using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskTitleMappingTests(ModelFixture fixture)
{
    [Fact]
    public void Title_is_a_non_nullable_bounded_varchar_mapped_through_TaskTitleConverter()
    {
        var property = fixture.GetRequiredProperty(nameof(TaskItem.Title));

        property.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(TaskTitle.MaxLength);
        // A dropped HasMaxLength would silently turn this into "text"; asserting the column type
        // itself, not just the facet, is what catches that.
        property.GetColumnType().ShouldBe($"character varying({TaskTitle.MaxLength})");

        // Exercised in both directions rather than asserted by runtime type name: HasConversion(lambda,
        // lambda) compiles to a compiler-generated ValueConverter<T,U> whose GetType().Name carries no
        // meaning.
        var converter = property.GetValueConverter().ShouldNotBeNull();
        converter.ProviderClrType.ShouldBe(typeof(string));
        converter.ConvertToProvider(TaskTitle.Create("Renew passport")).ShouldBe("Renew passport");
        converter.ConvertFromProvider("Renew passport").ShouldBeOfType<TaskTitle>().Value.ShouldBe("Renew passport");
    }

    [Fact]
    public void Titles_default_value_comparer_uses_value_equality_not_reference_equality()
    {
        var property = fixture.GetRequiredProperty(nameof(TaskItem.Title));
        var comparer = property.GetValueComparer().ShouldNotBeNull();

        var left = TaskTitle.Create("a");
        var right = TaskTitle.Create("a");

        // If TaskTitle were ever demoted from a record to a class, this is what would notice: EF's
        // default comparer would fall back to reference equality and every title assignment would
        // start looking like a change worth an UPDATE.
        ReferenceEquals(left, right).ShouldBeFalse();
        comparer.Equals(left, right).ShouldBeTrue();
    }
}

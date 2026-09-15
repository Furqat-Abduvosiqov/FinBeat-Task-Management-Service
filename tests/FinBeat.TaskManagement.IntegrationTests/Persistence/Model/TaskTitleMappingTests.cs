using FinBeat.TaskManagement.Domain.Tasks;
using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskTitleMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskTitleMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Fact]
    public void Title_is_a_non_nullable_bounded_varchar_mapped_through_TaskTitleConverter()
    {
        var property = _fixture.TaskEntityType.FindProperty(nameof(TaskItem.Title)).ShouldNotBeNull();

        property.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(TaskTitle.MaxLength);
        // A dropped HasMaxLength would silently turn this into "text"; asserting the column type
        // itself, not just the facet, is what catches that.
        property.GetColumnType().ShouldBe($"character varying({TaskTitle.MaxLength})");

        // By the converter's runtime type name rather than a compile-time reference: the converter is
        // an internal implementation detail of Infrastructure's persistence configuration.
        property.GetValueConverter().ShouldNotBeNull().GetType().Name.ShouldBe("TaskTitleConverter");
    }

    [Fact]
    public void Titles_default_value_comparer_uses_value_equality_not_reference_equality()
    {
        var property = _fixture.TaskEntityType.FindProperty(nameof(TaskItem.Title)).ShouldNotBeNull();
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

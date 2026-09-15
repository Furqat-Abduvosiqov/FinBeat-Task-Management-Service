using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

public sealed class TaskTimestampMappingTests : IClassFixture<ModelFixture>
{
    private readonly ModelFixture _fixture;

    public TaskTimestampMappingTests(ModelFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData(nameof(TaskItem.CreatedAt))]
    [InlineData(nameof(TaskItem.UpdatedAt))]
    public void Timestamp_is_a_non_nullable_DateTimeOffset_stored_with_time_zone(string propertyName)
    {
        var property = _fixture.TaskEntityType.FindProperty(propertyName).ShouldNotBeNull();

        property.ClrType.ShouldBe(typeof(DateTimeOffset));
        // "timestamp" alone reads and writes as if there were no offset at all - exactly the instant
        // semantics DateTimeOffset exists to keep. "with time zone" is what actually preserves them.
        property.GetColumnType().ShouldBe("timestamp with time zone");
        property.IsNullable.ShouldBeFalse();
    }
}

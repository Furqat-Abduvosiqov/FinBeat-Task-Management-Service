using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Model;

[Collection(nameof(ModelCollection))]
public sealed class TaskTimestampMappingTests(ModelFixture fixture)
{
    [Theory]
    [InlineData(nameof(TaskItem.CreatedAt))]
    [InlineData(nameof(TaskItem.UpdatedAt))]
    public void Timestamp_is_a_non_nullable_DateTimeOffset_stored_with_time_zone(string propertyName)
    {
        var property = fixture.GetRequiredProperty(propertyName);

        property.ClrType.ShouldBe(typeof(DateTimeOffset));
        // "timestamp" alone reads and writes as if there were no offset at all - exactly the instant
        // semantics DateTimeOffset exists to keep. "with time zone" is what actually preserves them.
        property.GetColumnType().ShouldBe("timestamp with time zone");
        property.IsNullable.ShouldBeFalse();
    }
}

using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskItemIdTests
{
    [Fact]
    public void From_with_empty_guid_throws()
    {
        Should.Throw<ArgumentException>(() => TaskItemId.From(Guid.Empty));
    }

    [Fact]
    public void From_with_a_non_empty_guid_wraps_it()
    {
        var value = Guid.NewGuid();

        var id = TaskItemId.From(value);

        id.Value.ShouldBe(value);
    }

    [Fact]
    public void New_produces_an_id_with_version_7_and_variant_10()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var id = TaskItemId.New(clock);
        var bytes = BigEndianBytes(id.Value);

        (bytes[6] >> 4).ShouldBe(7);
        (bytes[8] >> 6).ShouldBe(0b10);
    }

    [Fact]
    public void Ids_generated_at_increasing_times_sort_ascending()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var earlier = TaskItemId.New(clock);
        clock.Advance(TimeSpan.FromMilliseconds(5));
        var later = TaskItemId.New(clock);

        // Guid.CompareTo compares by internal field layout, not big-endian bytes, so it would not
        // reflect UUIDv7 ordering here. The "D" string form is big-endian by definition instead.
        var comparison = string.CompareOrdinal(earlier.Value.ToString("D"), later.Value.ToString("D"));
        comparison.ShouldBeLessThan(0);
    }

    [Fact]
    public void Ids_generated_at_the_same_instant_are_still_distinct()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);

        var first = TaskItemId.New(clock);
        var second = TaskItemId.New(clock);

        first.ShouldNotBe(second);
    }

    private static byte[] BigEndianBytes(Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes, bigEndian: true, out _);
        return bytes.ToArray();
    }
}

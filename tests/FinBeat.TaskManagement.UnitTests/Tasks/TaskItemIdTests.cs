using FinBeat.TaskManagement.Domain.Tasks;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FinBeat.TaskManagement.UnitTests.Tasks;

public class TaskItemIdTests
{
    // A realistic, non-zero instant. Using DateTimeOffset.UnixEpoch here would zero five of the six
    // timestamp bytes, which drains every assertion below of its power: with a zero timestamp,
    // reversing the byte order in TaskItemId.New to little-endian still yields ascending ids
    // (00 00 00 00 00 00 vs 05 00 00 00 00 00), so the ordering test would pass over a scrambled
    // layout. Keep this value non-zero in all six bytes.
    private static readonly DateTimeOffset Instant = new(2024, 5, 17, 10, 30, 0, TimeSpan.Zero);

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
    public void New_with_a_null_clock_throws()
    {
        Should.Throw<ArgumentNullException>(() => TaskItemId.New(null!));
    }

    [Fact]
    public void New_produces_an_id_with_version_7_and_variant_10()
    {
        var clock = new FakeTimeProvider(Instant);

        var id = TaskItemId.New(clock);
        var bytes = BigEndianBytes(id.Value);

        (bytes[6] >> 4).ShouldBe(7);
        (bytes[8] >> 6).ShouldBe(0b10);
    }

    // Pins the byte order, all six shift amounts, and the big-endian Guid construction in a single
    // assertion: any one of them going wrong changes the decoded value. The version/variant test
    // above cannot do this job — little-endian construction swaps bytes 6 and 7, so it would only
    // catch that defect probabilistically.
    [Fact]
    public void New_embeds_the_clock_instant_as_a_big_endian_48_bit_unix_millisecond_timestamp()
    {
        var clock = new FakeTimeProvider(Instant);

        var bytes = BigEndianBytes(TaskItemId.New(clock).Value);

        var embedded = 0L;
        for (var i = 0; i < 6; i++)
        {
            embedded = (embedded << 8) | bytes[i];
        }

        embedded.ShouldBe(Instant.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void Ids_generated_at_increasing_times_sort_ascending()
    {
        var clock = new FakeTimeProvider(Instant);

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
        var clock = new FakeTimeProvider(Instant);

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

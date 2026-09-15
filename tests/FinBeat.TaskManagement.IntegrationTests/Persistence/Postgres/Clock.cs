using Microsoft.Extensions.Time.Testing;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

/// <summary>The clock every Postgres round-trip test starts from.</summary>
internal static class Clock
{
    // A whole second, not DateTimeOffset.UnixEpoch: that would zero the UUIDv7 timestamp bytes and
    // mask the microsecond precision loss a real clock value would hit on round trip.
    private static readonly DateTimeOffset Instant = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    /// <summary>A fresh <see cref="FakeTimeProvider"/> pinned at a realistic instant, advancing a second on every read.</summary>
    /// <remarks>AutoAdvance, because a fixed <see cref="FakeTimeProvider"/> would stamp identical timestamps and "UpdatedAt moved" could never fail.</remarks>
    public static FakeTimeProvider New() => new(Instant) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };
}

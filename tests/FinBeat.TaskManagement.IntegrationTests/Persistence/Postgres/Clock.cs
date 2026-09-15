using Microsoft.Extensions.Time.Testing;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence.Postgres;

/// <summary>The clock every Postgres round-trip test starts from.</summary>
internal static class Clock
{
    // A whole second, deliberately. PostgreSQL's timestamptz keeps microseconds and .NET keeps
    // 100-nanosecond ticks, so a value from the real clock does not survive the round trip bit-exact
    // and an equality assertion on it flakes. Not DateTimeOffset.UnixEpoch - that zeroes the
    // timestamp bytes.
    private static readonly DateTimeOffset Instant = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// A fresh <see cref="FakeTimeProvider"/> pinned at a realistic instant, advancing a second on
    /// every read.
    /// </summary>
    /// <remarks>
    /// AutoAdvance, because a fixed <see cref="FakeTimeProvider"/> returns the same instant from every
    /// read: <c>Create</c> and <c>UpdateDetails</c> would stamp identical timestamps and "UpdatedAt
    /// moved" could not fail.
    /// </remarks>
    public static FakeTimeProvider New() => new(Instant) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };
}

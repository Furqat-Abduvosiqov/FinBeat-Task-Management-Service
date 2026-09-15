using Microsoft.Extensions.Time.Testing;

namespace FinBeat.TaskManagement.IntegrationTests.Persistence;

/// <summary>The clock every Postgres round-trip test starts from.</summary>
internal static class Clock
{
    // Microsecond-aligned, because timestamptz stores microseconds while .NET keeps 100-nanosecond
    // ticks: an instant with finer precision would not survive the round trip and equality assertions
    // on it would flake.
    private static readonly DateTimeOffset Instant = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    /// <summary>A fresh <see cref="FakeTimeProvider"/> pinned at a realistic instant, advancing a second on every read.</summary>
    /// <remarks>AutoAdvance, because a fixed <see cref="FakeTimeProvider"/> would stamp identical timestamps and "UpdatedAt moved" could never fail.</remarks>
    public static FakeTimeProvider New() => new(Instant) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };
}

using System;
using System.Diagnostics;

namespace NCrontab.Scheduler.Internals;

/// <summary>
///     Provides precise timing operations.
/// </summary>
public class DateTimePrecise
{
    private const long ClockTickFrequency = 10000000;

    private static long SynchronizePeriodStopwatchTicks;
    private static DateTimePreciseSafeImmutable Immutable;

    private static readonly DateTimePrecise StaticDtp = new();
    private readonly Stopwatch stopwatch;

    /// <summary>
    ///     Creates a new instance of <see cref="DateTimePrecise" />.
    /// </summary>
    private DateTimePrecise() : this(10) { }

    /// <summary>
    ///     Creates a new instance of <see cref="DateTimePrecise" />.
    /// </summary>
    /// <param name="synchronizePeriodSeconds">
    ///     The number of seconds after which the <see cref="DateTimePrecise" />
    ///     will synchronize itself with the system clock. A large value may
    ///     cause arithmetic overflow exceptions to be thrown. A small value may
    ///     cause the time to be unstable. A good default value is 10.
    /// </param>
    private DateTimePrecise(long synchronizePeriodSeconds)
    {
        this.stopwatch = Stopwatch.StartNew();
        this.stopwatch.Start();

        var utcNow = DateTime.UtcNow;
        Immutable = new DateTimePreciseSafeImmutable(utcNow, utcNow, this.stopwatch.ElapsedTicks, Stopwatch.Frequency);

        SynchronizePeriodStopwatchTicks = synchronizePeriodSeconds * Stopwatch.Frequency;
    }

    /// <summary>
    ///     Gets a <seealso cref="DateTime" /> object that is set to the current
    ///     precise date and time on this computer, expressed as the Coordinate
    ///     Universal Time (UTC).
    /// </summary>
    public static DateTime UtcNow => StaticDtp.UtcNowCustom;

    /// <summary>
    ///     Gets a <seealso cref="DateTime" /> object that is set to the current
    ///     precise date and time on this computer, expressed as the local time.
    /// </summary>
    public static DateTime Now => StaticDtp.NowCustom;

    /// Returns the current date and time, just like DateTime.UtcNow.
    private DateTime UtcNowCustom
    {
        get
        {
            var elapsedTicks = this.stopwatch.ElapsedTicks;
            var immutable = Immutable;

            if (elapsedTicks < immutable.SObserved + SynchronizePeriodStopwatchTicks)
            {
                return immutable.TBase.AddTicks((
                    elapsedTicks - immutable.SObserved) * ClockTickFrequency / immutable.StopWatchFrequency);
            }

            var t = DateTime.UtcNow;

            var tBaseNew = immutable.TBase.AddTicks((
                elapsedTicks - immutable.SObserved) * ClockTickFrequency / immutable.StopWatchFrequency);

            var stopWatchFrequency = (elapsedTicks - immutable.SObserved) * ClockTickFrequency * 2 / (t.Ticks -
                immutable.TObserved.Ticks + t.Ticks + t.Ticks - tBaseNew.Ticks - immutable.TObserved.Ticks);

            Immutable = new DateTimePreciseSafeImmutable(t, tBaseNew, elapsedTicks, stopWatchFrequency);

            return tBaseNew;
        }
    }

    /// <summary>
    ///     Returns the current date and time, just like DateTime.Now.
    /// </summary>
    private DateTime NowCustom => this.UtcNowCustom.ToLocalTime();

    private sealed class DateTimePreciseSafeImmutable
    {
        internal readonly long SObserved;
        internal readonly long StopWatchFrequency;
        internal readonly DateTime TBase;
        internal readonly DateTime TObserved;

        internal DateTimePreciseSafeImmutable(DateTime tObserved, DateTime tBase, long sObserved,
            long stopWatchFrequency)
        {
            this.TObserved = tObserved;
            this.TBase = tBase;
            this.SObserved = sObserved;
            this.StopWatchFrequency = stopWatchFrequency;
        }
    }
}
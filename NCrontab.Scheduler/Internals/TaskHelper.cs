using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCrontab.Scheduler.Internals
{
    internal static class TaskHelper
    {
        private static readonly TimeSpan MaxDelayPerIteration = TimeSpan.FromMilliseconds(int.MaxValue);
        internal static readonly TimeSpan InfiniteTimeSpan = TimeSpan.FromMilliseconds(-1);

        internal static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }

        internal static async Task LongDelay(IDateTime dateTime, TimeSpan delay, CancellationToken cancellationToken, TimeSpan maxDelayPerIteration = default)
        {
            if (delay == InfiniteTimeSpan)
            {
                await Task.Delay(delay, cancellationToken);
            }
            else
            {
                if (maxDelayPerIteration == default || maxDelayPerIteration > MaxDelayPerIteration)
                {
                    maxDelayPerIteration = MaxDelayPerIteration;
                }

                var startDateTime = dateTime.UtcNow;
                var remaining = delay;
                while (remaining > TimeSpan.Zero)
                {
                    if (remaining > maxDelayPerIteration)
                    {
                        remaining = maxDelayPerIteration;
                    }

                    await Task.Delay(remaining, cancellationToken);

                    remaining = (delay - (dateTime.UtcNow - startDateTime));
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}

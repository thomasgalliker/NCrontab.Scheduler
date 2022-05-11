using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NCrontab.Scheduler.Internals;
using Xunit;
using Xunit.Abstractions;

namespace NCrontab.Scheduler.Tests.Internals
{
    public class TaskHelperTests
    {
        private static readonly TimeSpan TaskDelayDeviation = TimeSpan.FromMilliseconds(30);

        private readonly ITestOutputHelper testOutputHelper;
        private readonly IDateTime dateTime;

        public TaskHelperTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
            this.dateTime = new SystemDateTime();
        }

        [Theory]
        [ClassData(typeof(LongDelayTestData))]
        public async Task ShouldAwaitTaskDelay(TimeSpan delay)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            await TaskHelper.Delay(delay, CancellationToken.None);

            // Assert
            var stopwatchElapsed = stopwatch.Elapsed;
            var deviation = stopwatchElapsed - delay;
            deviation.Should().BeCloseTo(TimeSpan.Zero, TaskDelayDeviation);
            this.testOutputHelper.WriteLine($"Task.Delay finished in {stopwatchElapsed} (deviation: {deviation.TotalMilliseconds:F0}ms)");
        }

        [Theory]
        [ClassData(typeof(LongDelayTestData))]
        public async Task ShouldAwaitLongDelay(TimeSpan delay)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            await TaskHelper.LongDelay(this.dateTime, delay, CancellationToken.None);

            // Assert
            var stopwatchElapsed = stopwatch.Elapsed;
            var deviation = stopwatchElapsed - delay;
            deviation.Should().BeCloseTo(TimeSpan.Zero, TaskDelayDeviation);
            this.testOutputHelper.WriteLine($"LongDelay finished in {stopwatchElapsed} (deviation: {deviation.TotalMilliseconds:F0}ms)");
        }

        [Fact]
        public async Task ShouldAwaitLongDelay_WithMultipleIterations()
        {
            // Arrange
            var delay = TimeSpan.FromSeconds(1.2d);
            var delayPerIteration = TimeSpan.FromSeconds(0.5d);
            var stopwatch = Stopwatch.StartNew();

            // Act
            await TaskHelper.LongDelay(this.dateTime, delay, CancellationToken.None, delayPerIteration);

            // Assert
            var stopwatchElapsed = stopwatch.Elapsed;
            var deviation = stopwatchElapsed - delay;
            deviation.Should().BeCloseTo(TimeSpan.Zero, TaskDelayDeviation);
            this.testOutputHelper.WriteLine($"LongDelay finished in {stopwatchElapsed} (deviation: {deviation.TotalMilliseconds:F0}ms)");
        }

        [Fact]
        public async Task ShouldAwaitLongDelay_ThrowsTaskCanceledException()
        {
            // Arrange
            var delay = TaskHelper.InfiniteTimeSpan;
            var cancellationToken = new CancellationTokenSource(1000);
            var stopwatch = Stopwatch.StartNew();

            // Act
            Func<Task> action = () => TaskHelper.LongDelay(this.dateTime, delay, cancellationToken.Token);

            // Assert
            await action.Should().ThrowAsync<TaskCanceledException>();
            this.testOutputHelper.WriteLine($"LongDelay finished in {stopwatch.Elapsed}");
        }

        internal class LongDelayTestData : TheoryData<TimeSpan>
        {
            public LongDelayTestData()
            {
                this.Add(TimeSpan.FromTicks(1));
                this.Add(TimeSpan.FromMilliseconds(1));
                this.Add(TimeSpan.FromSeconds(1));
                this.Add(TimeSpan.FromSeconds(10));
            }
        }
    }
}

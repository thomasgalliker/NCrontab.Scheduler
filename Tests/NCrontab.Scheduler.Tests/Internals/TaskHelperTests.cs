namespace NCrontab.Scheduler.Tests.Internals
{
    public class TaskHelperTests
    {
        private static readonly TimeSpan TaskDelayDeviation = TimeSpan.FromMilliseconds(100);

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
            this.testOutputHelper.WriteLine($"TaskHelper.Delay finished in {stopwatchElapsed} (deviation: {deviation.TotalMilliseconds:F0}ms)");
            deviation.Should().BeCloseTo(TimeSpan.Zero, TaskDelayDeviation);
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
            this.testOutputHelper.WriteLine($"TaskHelper.LongDelay finished in {stopwatchElapsed} (deviation: {deviation.TotalMilliseconds:F0}ms)");
            deviation.Should().BeCloseTo(TimeSpan.Zero, TaskDelayDeviation);
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
            this.testOutputHelper.WriteLine($"TaskHelper.LongDelay finished in {stopwatchElapsed} (deviation: {deviation.TotalMilliseconds:F0}ms)");
            deviation.Should().BeCloseTo(TimeSpan.Zero, TaskDelayDeviation);
        }

        [Fact]
        public async Task ShouldAwaitLongDelay_ThrowsTaskCanceledException()
        {
            // Arrange
            var delay = TaskHelper.InfiniteTimeSpan;
            var cancellationToken = new CancellationTokenSource(1000);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var action = () => TaskHelper.LongDelay(this.dateTime, delay, cancellationToken.Token);

            // Assert
            this.testOutputHelper.WriteLine($"TaskHelper.LongDelay finished in {stopwatch.Elapsed}");
            await action.Should().ThrowAsync<TaskCanceledException>();
        }

        private class LongDelayTestData : TheoryData<TimeSpan>
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
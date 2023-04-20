using System;
using FluentAssertions;
using Xunit;
namespace NCrontab.Scheduler.Tests
{
    /// <summary>
    /// Some tests just to demonstrate the capability to parse 6 and 7 digit cron expressions.
    /// </summary>
    public class CrontabScheduleTests
    {
        [Fact]
        public void ShouldRunEveryMinute()
        {
            // Arrange
            var now = new DateTime(2000, 1, 1, 12, 00, 00, DateTimeKind.Utc);
            var runEveryMinute = CrontabSchedule.Parse("* * * * *");

            // Act
            var next = runEveryMinute.GetNextOccurrence(now);

            // Assert
            next.Should().Be(new DateTime(2000, 1, 1, 12, 01, 00, DateTimeKind.Utc));
        }

        [Fact]
        public void ShouldRunEverySecond()
        {
            // Arrange
            var now = new DateTime(2000, 1, 1, 12, 00, 00, DateTimeKind.Utc);

            var options = new CrontabSchedule.ParseOptions
            {
                IncludingSeconds = true,
            };

            var runEverySecond = CrontabSchedule.Parse("* * * * * *", options);

            // Act
            var next = runEverySecond.GetNextOccurrence(now);

            // Assert
            next.Should().Be(new DateTime(2000, 1, 1, 12, 00, 01, DateTimeKind.Utc));
        }
    }
}

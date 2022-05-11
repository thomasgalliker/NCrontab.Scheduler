using System;
using FluentAssertions;
using NCrontab.Scheduler.Extensions;
using Xunit;

namespace NCrontab.Scheduler.Tests.Extensions
{
    public class TimeSpanExtensionsTests
    {
        [Fact]
        public void ShouldRoundUp()
        {
            // Arrange
            var maxRounding = TimeSpan.FromMilliseconds(100);
            
            var timeSpan = TimeSpan.FromHours(1)
                .Subtract(TimeSpan.FromTicks(100))
                .Subtract(TimeSpan.FromMilliseconds(90));

            // Act
            var roundedTimeSpan = timeSpan.RoundUp(maxRounding);


            // Assert
            roundedTimeSpan.Should().Be(TimeSpan.FromHours(1));
        }
    }
}
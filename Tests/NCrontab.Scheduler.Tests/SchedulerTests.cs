using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using NCrontab.Scheduler.Internals;
using NCrontab.Scheduler.Tests.Extensions;
using NCrontab.Scheduler.Tests.Logging;
using NCrontab.Scheduler.Tests.TestData;
using Xunit;
using Xunit.Abstractions;

namespace NCrontab.Scheduler.Tests
{
    public class SchedulerTests
    {
        private readonly AutoMocker autoMocker;

        public SchedulerTests(ITestOutputHelper testOutputHelper)
        {
            this.autoMocker = new AutoMocker();
            this.autoMocker.Use<ILogger<Scheduler>>(new TestOutputHelperLogger<Scheduler>(testOutputHelper));
        }

        [Fact]
        public async Task ShouldStartWithNoTasksAdded()
        {
            // Arrange
            var nextCount = 0;
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.Next += (sender, args) => { nextCount++; };

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1000))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Assert
            scheduler.IsRunning.Should().BeFalse();
            nextCount.Should().Be(0);
        }

        [Fact]
        public async Task ShouldAddTask_SingleTask_Synchronous()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now).Returns(new DateTime(2019, 11, 06, 14, 43, 59));
            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var actionObject = new TestObject();
            scheduler.AddTask("* * * * *", (cancellationToken) =>
            {
                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1100))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            actionObject.RunCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldAddTask_SingleTask_Asynchronous()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now).Returns(new DateTime(2019, 11, 06, 14, 43, 59));
            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var actionObject = new TestObject();
            scheduler.AddTask("* * * * *", async (cancellationToken) =>
            {
                await actionObject.RunAsync();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1100))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            actionObject.RunCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldAddTask_MultipleTasks()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now).Returns(new DateTime(2019, 11, 06, 14, 43, 59));
            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var actionObject1 = new TestObject();
            scheduler.AddTask("* * * * *", (cancellationToken) =>
            {
                actionObject1.Run();
            });

            var actionObject2 = new TestObject();
            scheduler.AddTask("* * * * *", (cancellationToken) =>
            {
                actionObject2.Run();
            });

            var actionObject3 = new TestObject();
            scheduler.AddTask("* * * * *", async (cancellationToken) =>
            {
                await actionObject3.RunAsync();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1100))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            actionObject1.RunCount.Should().Be(1);
            actionObject2.RunCount.Should().Be(1);
            actionObject3.RunCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldAddTask_Concurrent()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var recordedNextEvents = new List<ScheduledEventArgs>();
            scheduler.Next += (sender, args) => { recordedNextEvents.Add(args); };

            var numberOfTasks = 20;

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1000))
            {
                var startTask = Task.Run(async () =>
                {
                    await scheduler.StartAsync(cancellationTokenSource.Token);
                });

                // Adding tasks concurrently
                var taskIds = Enumerable.Range(0, numberOfTasks).Select(i => Guid.NewGuid()).ToList();
                Parallel.ForEach(taskIds, id => scheduler.AddTask(id, "* * * * *", ct => { }));

                // Removing tasks concurrently
                Parallel.ForEach(taskIds, id => scheduler.RemoveTask(id));

                await startTask;
            }

            // Assert
            recordedNextEvents.Count.Should().BeInRange(0, 2);
        }

        [Fact]
        public async Task ScheduleMultipleJobs_2()
        {
            // Arrange
            var referenceDate = new DateTime(2000, 1, 1, 22, 59, 58);

            var clockQueue = new DateTimeGenerator(
                referenceDate,
                new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromMinutes(59) + TimeSpan.FromSeconds(58),
                });

            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.Now, referenceDate, (n) => clockQueue.GetNext());

            var tcs = new TaskCompletionSource();
            var recordedNextEvents = new List<ScheduledEventArgs>();

            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.Next += (s, e) => { recordedNextEvents.Add(e); if (recordedNextEvents.Count == 2) { tcs.SetResult(); } };

            var testObjectDaily = new TestObject();
            scheduler.AddTask("0 0 * * *", _ => testObjectDaily.Run());

            var testObjectHourly = new TestObject();
            scheduler.AddTask("0 * * * *", _ => testObjectHourly.Run());

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(60000))
            {
                scheduler.Start(cancellationTokenSource.Token);
                await tcs.Task;
            }

            // Arrange
            recordedNextEvents.Should().HaveCount(2);
            recordedNextEvents[0].SignalTime.Should().Be(new DateTime(2000, 1, 1, 23, 00, 00));
            recordedNextEvents[1].SignalTime.Should().Be(new DateTime(2000, 1, 2, 00, 00, 00));
            testObjectHourly.RunCount.Should().Be(2);
            testObjectDaily.RunCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldStopSchedulingIfTaskIsRemoved()
        {
            // Arrange
            var nextCount = 0;
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.Next += (sender, args) => { nextCount++; };

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(3000))
            {
                var startTask = Task.Run(async () =>
                {
                    await scheduler.StartAsync(cancellationTokenSource.Token);
                });

                await Task.Delay(100);

                var taskId = scheduler.AddTask("* * * * *", ct => { });

                await Task.Delay(1100);

                scheduler.RemoveTask(taskId);

                await startTask;
            }

            // Assert
            nextCount.Should().Be(1);
        }

        [Fact(Skip = "to be fixed")]
        public async Task ScheduleJobThatWillTakeMoreThanAMinuteToRunAndLogWarning()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 58))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 44, 01))
                .Returns(new DateTime(2019, 11, 06, 14, 45, 02));

            var logger = new Mock<ILogger<Scheduler>>();

            var scheduler = new Scheduler(logger.Object, dateTimeMock.Object);

            var actionObject = new TestObject();
            scheduler.AddTask("44 14 * * *", (cancellationToken) =>
            {
                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(2000))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Assert
            logger.Verify(x => x.Log(LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals("Execution took more than one minute", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact(Skip = "to be fixed")]
        public async Task ScheduleJobThatShouldRunInNextMinuteButChangeThatBeforeThatSoNoExecutionHaveOccured()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 58))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));
            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var actionObject = new TestObject();
            var id = scheduler.AddTask("44 14 * * * 2019", (cancellationToken) =>
            {
                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1100))
            {
                var task = Task.Run(async () =>
                {
                    await scheduler.StartAsync(cancellationTokenSource.Token);
                });

                scheduler.ChangeScheduleAndResetScheduler(id, CrontabSchedule.Parse("50 14 * * * 2019"));

                await task;
            }

            // Assert
            actionObject.RunCount.Should().Be(0);
        }

        [Fact(Skip = "to be fixed")]
        public async Task ScheduleJobThatShouldRunInNextMinuteButChangeThatBeforeThatSoNoExecutionHaveOccuredWithExternalId()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 58))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));
            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var id = Guid.NewGuid();
            var actionObject = new TestObject();
            scheduler.AddTask(id, "44 14 * * * 2019", (cancellationToken) =>
            {
                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1100))
            {
                var task = Task.Run(async () =>
                {
                    await scheduler.StartAsync(cancellationTokenSource.Token);
                });

                scheduler.ChangeScheduleAndResetScheduler(id, CrontabSchedule.Parse("50 14 * * * 2019"));

                await task;
            }

            // Arrange
            actionObject.RunCount.Should().Be(0);
        }

        [Fact(Skip = "to be fixed")]
        public async Task ScheduleJobThatShouldRunInNextMinuteButStopSchedulerBeforeThatSoNoExecutionHaveOccured()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(mock => mock.Now)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 58))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));
            var scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var internalStopInvoked = false;
            var actionObject = new TestObject();
            var id = scheduler.AddTask("44 14 * * * 2019", async (cancellationToken) =>
            {
                var continueExecutionAfterDelay = await Task.Delay(10000, cancellationToken).ContinueWith(task =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }
                    return true;
                });

                if (!continueExecutionAfterDelay)
                {
                    internalStopInvoked = true;
                    return;
                }

                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(3100))
            {
                var task = Task.Run(async () =>
                {
                    await scheduler.StartAsync(cancellationTokenSource.Token);
                });

                await Task.Delay(2000);

                scheduler.Stop();

                await task;
            }

            // Arrange
            Assert.True(internalStopInvoked);
            actionObject.RunCount.Should().Be(0);
        }

        [Fact]
        public async Task ScheduleAsyncJobsAndOneWillFailTheOtherWillStillRunAndLogWillBeCreated()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(mock => mock.Now).Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            var logger = new Mock<ILogger<Scheduler>>();

            var scheduler = new Scheduler(logger.Object, dateTimeMock.Object);

            var actionObject = new TestObject();
            scheduler.AddTask(CrontabSchedule.Parse("* * * * *"), async (cancellationToken) =>
            {
                await actionObject.RunAsync();
            });

            var failingTaskId = scheduler.AddTask(CrontabSchedule.Parse("* * * * *"), (cancellationToken) =>
            {
                throw new Exception("Fail!!");
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(1100))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            actionObject.RunCount.Should().Be(1);
            logger.Verify(x => x.Log(LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Task with Id={failingTaskId:B} failed with exception")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

    }
}

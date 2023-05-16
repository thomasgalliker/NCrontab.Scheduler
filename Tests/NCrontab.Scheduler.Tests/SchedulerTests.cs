using System;
using System.Collections.Concurrent;
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
        private readonly ITestOutputHelper testOutputHelper;
        private readonly AutoMocker autoMocker;

        public SchedulerTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;

            this.autoMocker = new AutoMocker();
            this.autoMocker.Use<ILogger<Scheduler>>(new TestOutputHelperLogger<Scheduler>(testOutputHelper));

            var schedulerOptionsMock = this.autoMocker.GetMock<ISchedulerOptions>();
            schedulerOptionsMock.SetupGet(o => o.DateTimeKind)
                .Returns(DateTimeKind.Utc);
        }

        [Fact]
        public async Task ShouldStartWithNoTasksAdded()
        {
            // Arrange
            var nextCount = 0;
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
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
            var referenceDate = new DateTime(2019, 11, 06, 14, 43, 58);
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow, referenceDate, (n) => n.AddSeconds(1));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var crontabSchedule = CrontabSchedule.Parse("* * * * *");
            var actionObject = new TestObject();
            scheduler.AddTask(crontabSchedule, (cancellationToken) =>
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
        public async Task ShouldAddTask_SingleTask_Synchronous_ParseIncludingSeconds()
        {
            // Arrange
            var referenceDate = new DateTime(2019, 11, 06, 14, 43, 58);
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow, referenceDate, (n) => n.AddSeconds(1));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var options = new CrontabSchedule.ParseOptions
            {
                IncludingSeconds = true,
            };
            var crontabSchedule = CrontabSchedule.Parse("* * * * * *", options);
            var actionObject = new TestObject();
            scheduler.AddTask(crontabSchedule, (cancellationToken) =>
            {
                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(11000))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            actionObject.RunCount.Should().Be(10);
        }

        [Fact]
        public async Task ShouldAddTask_SingleTask_Synchronous_WithLocalTime()
        {
            // Arrange
            var referenceDate = new DateTime(2000, 1, 1, 16, 33, 58, DateTimeKind.Utc);
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.Now, referenceDate, (n) => n.AddSeconds(1));
            dateTimeMock.SetupSequence(d => d.UtcNow, referenceDate.ToLocalTime(), (n) => n.AddSeconds(1));

            var schedulerOptionsMock = this.autoMocker.GetMock<ISchedulerOptions>();
            schedulerOptionsMock.SetupGet(o => o.DateTimeKind)
                .Returns(DateTimeKind.Local);

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var crontabSchedule = CrontabSchedule.Parse("0 12 1 11 *");
            var next1 = crontabSchedule.GetNextOccurrence(new DateTime(2023, 01, 01, 11, 00, 00, DateTimeKind.Local));
            var next2 = crontabSchedule.GetNextOccurrence(new DateTime(2023, 05, 12, 11, 00, 00, DateTimeKind.Local));
            var next3 = crontabSchedule.GetNextOccurrence(new DateTime(2023, 11, 01, 11, 00, 00, DateTimeKind.Local));

            var actionObject = new TestObject();
            scheduler.AddTask(crontabSchedule, (cancellationToken) =>
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
            var referenceDate = new DateTime(2019, 11, 06, 14, 43, 58);
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow, referenceDate, (n) => n.AddSeconds(1));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

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
            var referenceDate = new DateTime(2019, 11, 06, 14, 43, 58);
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow, referenceDate, (n) => n.AddSeconds(1));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

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
        public async Task ShouldAddTask_ConcurrentAddAndRemove()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
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
        public void ShouldRemoveTasks_ByTaskIds()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var taskId1 = scheduler.AddTask("*/1 * * * *", (cancellationToken) => { });
            var taskId2 = scheduler.AddTask("*/2 * * * *", (cancellationToken) => { });
            var taskId3 = Guid.NewGuid();
            var taskIds = new[] { taskId1, taskId2, taskId3 };

            // Act
            var results = scheduler.RemoveTasks(taskIds);

            // Arrange
            results.Should().HaveCount(taskIds.Length);
            results.Should().Contain(t => t.TaskId == taskId1 && t.Removed == true);
            results.Should().Contain(t => t.TaskId == taskId2 && t.Removed == true);
            results.Should().Contain(t => t.TaskId == taskId3 && t.Removed == false);
        }

        [Fact]
        public void ShouldRemoveTasks_ByTasks()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var task1 = new ScheduledTask("*/1 * * * *", (cancellationToken) => { });
            scheduler.AddTask(task1);
            
            var task2 = new ScheduledTask("*/2 * * * *", (cancellationToken) => { });
            scheduler.AddTask(task2);

            var task3 = new ScheduledTask("*/3 * * * *", (cancellationToken) => { });
            var tasks = new[] { task1, task2, task3 };

            // Act
            var results = scheduler.RemoveTasks(tasks);

            // Arrange
            results.Should().HaveCount(tasks.Length);
            results.Should().Contain(t => t.TaskId == task1.Id && t.Removed == true);
            results.Should().Contain(t => t.TaskId == task2.Id && t.Removed == true);
            results.Should().Contain(t => t.TaskId == task3.Id && t.Removed == false);
        }

        [Fact]
        public void ShouldGetTasks()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var taskId1 = scheduler.AddTask("*/1 * * * *", (cancellationToken) => { });
            var taskId2 = scheduler.AddTask("*/2 * * * *", (cancellationToken) => { });
            var taskId3 = scheduler.AddTask("*/3 * * * *", (cancellationToken) => { });

            // Act
            var tasks = scheduler.GetTasks().ToList();
            var task1 = scheduler.GetTaskById(taskId1);
            var task2 = scheduler.GetTaskById(taskId1);
            var task3 = scheduler.GetTaskById(taskId1);

            // Arrange
            tasks.Should().HaveCount(3);
            tasks.Should().Contain(t => t.Id == task1.Id);
            tasks.Should().Contain(t => t.Id == task2.Id);
            tasks.Should().Contain(t => t.Id == task3.Id);
        }

        [Fact]
        public void ShouldGetNextOccurrences_WithStartDate()
        {
            // Arrange
            var startDate = new DateTime(2000, 1, 1, 0, 0, 0);

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var taskId1 = scheduler.AddTask("*/1 * * * *", (cancellationToken) => { });
            var taskId2 = scheduler.AddTask("*/2 * * * *", (cancellationToken) => { });
            var taskId3 = scheduler.AddTask("*/3 * * * *", (cancellationToken) => { });

            // Act
            var nexts = scheduler.GetNextOccurrences(startDate).ToList();

            // Arrange
            nexts.Should().HaveCount(3);
        }

        [Fact]
        public void ShouldGetNextOccurrences_WithStartDateAndEndDate()
        {
            // Arrange
            var startDate = new DateTime(2000, 1, 1, 0, 0, 0);
            var endDate = startDate.AddHours(1);

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var taskId1 = scheduler.AddTask("*/1 * * * *", (cancellationToken) => { });
            var taskId2 = scheduler.AddTask("*/2 * * * *", (cancellationToken) => { });
            var taskId3 = scheduler.AddTask("*/3 * * * *", (cancellationToken) => { });

            // Act
            var nexts = scheduler.GetNextOccurrences(startDate, endDate).ToList();

            // Arrange
            nexts.Should().HaveCount(59);
            nexts.Where(n => n.ScheduledTasks.Count() == 0).Should().HaveCount(0);
            nexts.Where(n => n.ScheduledTasks.Count() == 1).Should().HaveCount(20);
            nexts.Where(n => n.ScheduledTasks.Count() == 2).Should().HaveCount(30);
            nexts.Where(n => n.ScheduledTasks.Count() == 3).Should().HaveCount(9);
            nexts.Where(n => n.ScheduledTasks.Count() > 3).Should().HaveCount(0);
        }

        [Fact]
        public async Task ShouldRemoveAllTasks()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var recordedNextEvents = new List<ScheduledEventArgs>();
            scheduler.Next += (sender, args) => { recordedNextEvents.Add(args); };

            var numberOfTasks = 10;

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(2000))
            {
                var startTask = Task.Run(async () =>
                {
                    await scheduler.StartAsync(cancellationTokenSource.Token);
                });

                // Adding tasks concurrently
                var taskIds = Enumerable.Range(0, numberOfTasks).Select(i => Guid.NewGuid()).ToList();
                Parallel.ForEach(taskIds, id => scheduler.AddTask(id, "* * * * *", ct => { }));

                // Removing all tasks at once
                scheduler.RemoveAllTasks();

                await startTask;
            }

            // Assert
            recordedNextEvents.Count.Should().Be(0);
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
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.Zero,
                    TimeSpan.FromMinutes(59) + TimeSpan.FromSeconds(59),
                    TimeSpan.Zero,
                });

            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow, referenceDate, (n) => clockQueue.GetNext());

            var tcs = new TaskCompletionSource();
            var recordedNextEvents = new ConcurrentBag<ScheduledEventArgs>();

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
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
                scheduler.Stop();
            }

            // Arrange
            this.testOutputHelper.WriteLine($"{ObjectDumper.Dump(recordedNextEvents, DumpStyle.CSharp)}");

            recordedNextEvents.Should().HaveCount(2);
            recordedNextEvents.Should().ContainSingle(e => e.SignalTime == new DateTime(2000, 1, 1, 23, 00, 00) && e.TaskIds.Length == 1);
            recordedNextEvents.Should().ContainSingle(e => e.SignalTime == new DateTime(2000, 1, 2, 00, 00, 00) && e.TaskIds.Length == 2);
            testObjectHourly.RunCount.Should().Be(2);
            testObjectDaily.RunCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldRemoveTask()
        {
            // Arrange
            var nextCount = 0;
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 44, 00));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.Next += (sender, args) => { nextCount++; };

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(4000))
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

        [Fact]
        public async Task ScheduleJobThatWillTakeMoreThanAMinuteToRunAndLogWarning()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 44, 00))
                .Returns(new DateTime(2019, 11, 06, 14, 44, 00))
                .Returns(new DateTime(2019, 11, 06, 14, 45, 00));

            var logger = new Mock<ILogger<Scheduler>>();
            var schedulerOptionsMock = this.autoMocker.GetMock<ISchedulerOptions>();

            IScheduler scheduler = new Scheduler(logger.Object, dateTimeMock.Object, schedulerOptionsMock.Object);

            var actionObject = new TestObject();
            scheduler.AddTask("44 14 * * *", (cancellationToken) =>
            {
                actionObject.Run();
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(2100))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Assert
            logger.Verify(x => x.Log(LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().StartsWith("Execution finished after")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleJobThatShouldRunInNextMinuteButChangeThatBeforeThatSoNoExecutionHaveOccured()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 58))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var actionObject = new TestObject();
            var taskId = scheduler.AddTask("44 14 * * *", (cancellationToken) =>
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

                scheduler.UpdateTask(taskId, CrontabSchedule.Parse("50 14 * * *"));

                await task;
            }

            // Assert
            actionObject.RunCount.Should().Be(0);
        }

        [Fact]
        public async Task ScheduleJobThatShouldRunInNextMinuteButChangeThatBeforeThatSoNoExecutionHaveOccuredWithExternalId()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 58))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var id = Guid.NewGuid();
            var actionObject = new TestObject();
            scheduler.AddTask(id, "44 14 * * *", (cancellationToken) =>
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

                scheduler.UpdateTask(id, CrontabSchedule.Parse("50 14 * * *"));

                await task;
            }

            // Arrange
            actionObject.RunCount.Should().Be(0);
        }

        [Fact]
        public async Task ScheduleJobThatShouldRunInNextMinuteButStopSchedulerBeforeThatSoNoExecutionHaveOccured()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 44, 00));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);

            var testObject = new TestObject();
            scheduler.AddTask("44 14 * * *", async (cancellationToken) =>
            {
                try
                {
                    await Task.Delay(10000, cancellationToken);
                    testObject.Run();
                }
                catch (Exception ex)
                {
                    testObject.Catch(ex);
                }
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(3000))
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
            testObject.RunCount.Should().Be(0);
            testObject.Exceptions.Should().ContainSingle(e => e is TaskCanceledException);
        }

        [Fact]
        public async Task ScheduleAsyncJobsAndOneWillFailTheOtherWillStillRunAndLogWillBeCreated()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.SetupSequence(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59))
                .Returns(new DateTime(2019, 11, 06, 14, 44, 00));

            var logger = new Mock<ILogger<Scheduler>>();
            var schedulerOptionsMock = this.autoMocker.GetMock<ISchedulerOptions>();

            IScheduler scheduler = new Scheduler(logger.Object, dateTimeMock.Object, schedulerOptionsMock.Object);

            var testObject1 = new TestObject();
            scheduler.AddTask(CrontabSchedule.Parse("* * * * *"), async (cancellationToken) =>
            {
                await testObject1.RunAsync();
            });

            var testObject2 = new TestObject();
            var failingTaskId = scheduler.AddTask(CrontabSchedule.Parse("* * * * *"), (cancellationToken) =>
            {
                testObject2.Run();
                throw new Exception("Fail!!");
            });

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(2100))
            {
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            logger.Verify(x => x.Log(LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Task with Id={failingTaskId:B} failed with exception")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            testObject1.RunCount.Should().Be(1);
            testObject2.RunCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldStop_CancelsScheduledTasksAndStopsScheduler()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.AddTask("* * * * *", (c) => Task.CompletedTask);

            scheduler.Start();
            await Task.Delay(500);

            // Act
            scheduler.Stop();

            // Arrange
            scheduler.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldStopAndRestart()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.AddTask("* * * * *", (c) => Task.CompletedTask);

            // Act
            using (var cancellationTokenSource = new CancellationTokenSource(2100))
            {
                scheduler.Start();
                await Task.Delay(500);
                scheduler.Stop();
                await Task.Delay(500);
                await scheduler.StartAsync(cancellationTokenSource.Token);
            }

            // Arrange
            scheduler.IsRunning.Should().BeFalse();

            var tasks = scheduler.GetTasks();
            tasks.Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldDispose_CancelsAndRemovesScheduledTasksAndStopsScheduler()
        {
            // Arrange
            var dateTimeMock = this.autoMocker.GetMock<IDateTime>();
            dateTimeMock.Setup(d => d.UtcNow)
                .Returns(new DateTime(2019, 11, 06, 14, 43, 59));

            IScheduler scheduler = this.autoMocker.CreateInstance<Scheduler>(enablePrivate: true);
            scheduler.AddTask("* * * * *", (c) => Task.CompletedTask);

            scheduler.Start();
            await Task.Delay(500);

            // Act
            scheduler.Dispose();

            // Arrange
            scheduler.IsRunning.Should().BeFalse();

            var tasks = scheduler.GetTasks();
            tasks.Should().BeEmpty();
        }
    }
}

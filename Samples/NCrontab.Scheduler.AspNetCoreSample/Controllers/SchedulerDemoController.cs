using Microsoft.AspNetCore.Mvc;

namespace NCrontab.Scheduler.AspNetCoreSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchedulerDemoController : ControllerBase
    {
        private readonly ILogger<SchedulerDemoController> logger;
        private readonly IScheduler scheduler;

        public SchedulerDemoController(
            ILogger<SchedulerDemoController> logger,
            IScheduler scheduler)
        {
            this.logger = logger;
            this.scheduler = scheduler;
        }

        [HttpGet("start")]
        public void Start()
        {
            this.scheduler.Start();
        }

        [HttpGet("stop")]
        public void Stop()
        {
            this.scheduler.Stop();
        }

        [HttpPost("addtask")]
        public Guid AddTask(string name, string cronExpression = "* * * * *")
        {
            var scheduledTask = new ScheduledTask(
                name,
                CrontabSchedule.Parse(cronExpression),
                action: ct => { this.logger.LogInformation($"Action executed!"); });

            this.scheduler.AddTask(scheduledTask);

            return scheduledTask.Id;
        }

        [HttpDelete("removetask")]
        public bool RemoveTask(Guid taskId)
        {
            return this.scheduler.RemoveTask(taskId);
        }

        [HttpDelete("removealltasks")]
        public void RemoveAllTask()
        {
            this.scheduler.RemoveAllTasks();
        }
    }
}
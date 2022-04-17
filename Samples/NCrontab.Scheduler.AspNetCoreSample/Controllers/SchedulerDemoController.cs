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

        [HttpPost("addtask")]
        public void AddTaskEveryMinute(string cronExpression = "* * * * *")
        {
            this.scheduler.AddTask(
              cronExpression: CrontabSchedule.Parse("* * * * *"),
              action: ct => { this.logger.LogInformation($"{DateTime.Now:O} -> Task runs every minutes"); });
        }

        [HttpDelete("removealltasks")]
        public void RemoveAllTask()
        {
            this.scheduler.RemoveAllTasks();
        }
    }
}
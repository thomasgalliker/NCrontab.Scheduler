namespace NCrontab.Scheduler.AspNetCoreSample.Tasks
{
    public class NightlyTask : TaskBase, IScheduledTask
    {
        private readonly ILogger logger;

        public NightlyTask(ILogger<NightlyTask> logger)
            : base("NightlyTask", CrontabSchedule.Parse("0 0 * * *"))
        {
            this.logger = logger;
        }

        public void Run(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{DateTime.Now:O} -> Run (Id={this.Id:B})");
        }
    }
}
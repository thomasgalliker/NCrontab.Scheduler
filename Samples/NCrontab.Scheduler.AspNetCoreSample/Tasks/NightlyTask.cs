namespace NCrontab.Scheduler.AspNetCoreSample.Tasks
{
    public class NightlyTask : IScheduledTask
    {
        private readonly ILogger logger;

        public NightlyTask(ILogger<NightlyTask> logger)
        {
            this.logger = logger;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public CrontabSchedule CrontabSchedule { get; set; } = CrontabSchedule.Parse("0 0 * * *");

        public void Run(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{DateTime.Now:O} -> Run (Id={this.Id:B})");
        }
    }
}
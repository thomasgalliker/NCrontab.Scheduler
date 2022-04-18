namespace NCrontab.Scheduler.AspNetCoreSample.Tasks
{
    public class NightlyAsyncTask : IAsyncScheduledTask
    {
        private readonly ILogger logger;

        public NightlyAsyncTask(ILogger<NightlyAsyncTask> logger)
        {
            this.logger = logger;
        }

        public CrontabSchedule CrontabSchedule { get; set; } = CrontabSchedule.Parse("0 0 * * *");

        public Guid Id { get; } = Guid.NewGuid();

        public Task RunAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{DateTime.Now:O} -> RunAsync (Id={this.Id:B})");
            return Task.CompletedTask;
        }
    }
}
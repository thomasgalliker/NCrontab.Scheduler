namespace NCrontab.Scheduler.AspNetCoreSample.Tasks
{
    public class NightlyAsyncTask : TaskBase, IAsyncScheduledTask
    {
        private readonly ILogger logger;

        public NightlyAsyncTask(ILogger<NightlyAsyncTask> logger)
            : base("NightlyAsyncTask", CrontabSchedule.Parse("0 0 * * *"))
        {
            this.logger = logger;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{DateTime.Now:O} -> RunAsync (Id={this.Id:B})");
            return Task.CompletedTask;
        }
    }
}
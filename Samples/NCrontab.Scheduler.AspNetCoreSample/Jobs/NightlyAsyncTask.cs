namespace NCrontab.Scheduler.AspNetCoreSample
{
    internal class NightlyAsyncTask : IAsyncTask
    {
        private readonly ILogger logger;

        public NightlyAsyncTask(ILogger<NightlyAsyncTask> logger)
        {
            this.logger = logger;
        }

        public string CronExpression => "0 0 * * *";

        public Task RunAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{DateTime.Now:O} -> RunAsync");
            return Task.CompletedTask;
        }
    }
}
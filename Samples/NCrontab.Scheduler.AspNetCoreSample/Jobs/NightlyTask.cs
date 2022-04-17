namespace NCrontab.Scheduler.AspNetCoreSample
{
    internal class NightlyTask : ITask
    {
        private readonly ILogger logger;

        public NightlyTask(ILogger<NightlyTask> logger)
        {
            this.logger = logger;
        }

        public string CronExpression => "0 0 * * *";

        public void Run(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{DateTime.Now:O} -> Run");
        }
    }
}
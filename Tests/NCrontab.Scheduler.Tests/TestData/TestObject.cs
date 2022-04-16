using System.Threading.Tasks;

namespace NCrontab.Scheduler.Tests.TestData
{
    public class TestObject
    {
        public int RunCount { get; private set; }

        public string CronExpression { get; set; }

        public void Run()
        {
            this.RunCount++;
        }

        public Task RunAsync()
        {
            this.Run();

            return Task.CompletedTask;
        }
    }
}

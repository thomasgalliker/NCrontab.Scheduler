using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCrontab.Scheduler.Tests.TestData
{
    public class TestObject
    {
        private readonly ICollection<Exception> exceptions;

        public TestObject()
        {
            this.exceptions = new List<Exception>();
        }

        public int RunCount { get; private set; }

        public IEnumerable<Exception> Exceptions => this.exceptions;

        public void Run()
        {
            this.RunCount++;
        }

        public Task RunAsync()
        {
            this.Run();

            return Task.CompletedTask;
        }

        public void Catch(Exception ex)
        {
            this.exceptions.Add(ex);
        }
    }
}

using System;
using System.Linq.Expressions;
using Moq;

namespace NCrontab.Scheduler.Tests.Extensions
{
    public static class MockExtensions
    {
        /// <summary>
        /// Recursively sets up a new expression.
        /// </summary>
        public static TReturn SetupSequence<TMock, TReturn>(this Mock<TMock> mock, Expression<Func<TMock, TReturn>> expression, TReturn value, Func<TReturn, TReturn> nextValue) where TMock : class
        {
            mock.SetupSequence(expression)
                .Returns(() => mock.SetupSequence(expression, nextValue(value), nextValue));

            return value;
        }
    }
}

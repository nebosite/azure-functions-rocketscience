using Microsoft.Azure.Functions.AFRocketScience;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScienceTests
{
    [ExcludeFromCodeCoverage]
    class MockLogger : ILogger
    {
        public List<string> Errors = new List<string>();

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            throw new NotImplementedException();
        }

        public void LogError(string message, params object[] items)
        {
            Errors.Add(message + " * " + ((Exception)items[0]).Message);
        }
    }
}

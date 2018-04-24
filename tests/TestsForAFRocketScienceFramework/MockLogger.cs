using Microsoft.Azure.Functions.AFRocketScience;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScienceTests
{
    [ExcludeFromCodeCoverage]
    class MockLogger : IServiceLogger
    {
        public List<string> Errors = new List<string>();
        public List<string> Infos = new List<string>();

        public void Error(string message, Exception exception)
        {
            Errors.Add(message + " * " + exception.Message);
        }

        public void Info(string message)
        {
            Infos.Add(message);
        }
    }
}

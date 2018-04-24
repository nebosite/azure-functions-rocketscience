using Microsoft.Azure.Functions.AFRocketScience;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggerExample
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// We create a thin wrapper around Tracewriter to help isolate our service code
    /// from the Azure Functions framework, which can cause DLL compatability problems.
    /// </summary>
    //--------------------------------------------------------------------------------
    class TraceLogger : IServiceLogger
    {
        TraceWriter _writer;
        public TraceLogger(TraceWriter writer) { _writer = writer; }
        public void Error(string message, Exception exception) { _writer.Error(message, exception); }
        public void Info(string message) {  _writer.Info(message);  }
    }
}

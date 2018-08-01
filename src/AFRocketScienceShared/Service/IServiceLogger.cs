
using System;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Generic logging interface to isolate us from TraceWriter
    /// </summary>
    //--------------------------------------------------------------------------------
    public interface IServiceLogger
    {
        void Error(string message, Exception exception);
        void Info(string message);
    }
}

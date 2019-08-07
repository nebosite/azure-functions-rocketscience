using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// This interface isolates us from the logic that reads the web server environment.
    /// </summary>
    //--------------------------------------------------------------------------------
    public interface IRocketScienceRequest
    {
        string Host { get; }
        string LocalPath { get;  }
        string Key { get; }
        string Content { get; }
        KeyValuePair<string, string>[] QueryParts { get; }
        KeyValuePair<string, string>[] Headers { get; }
    }
}

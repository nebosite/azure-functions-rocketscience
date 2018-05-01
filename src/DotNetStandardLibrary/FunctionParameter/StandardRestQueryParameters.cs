using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Typical parameters available on any query interface
    /// </summary>
    //--------------------------------------------------------------------------------
    public class StandardRestQueryParameters
    {
        /// <summary>
        /// Translates to $top.  (Total number of records to return) 
        /// </summary>
        [FunctionParameter(SourcePropertyName = "$Top" )]
        public int Query_Top { get; set; }

        /// <summary>
        /// Translates to $skip.   (Where to start returning records)
        /// </summary>
        [FunctionParameter(SourcePropertyName = "$Skip")]
        public int Query_Skip { get; set; }

        /// <summary>
        /// Translates to $count.  (If true, then get the count of records)
        /// </summary>
        [FunctionParameter(SourcePropertyName = "$Count")]
        public bool Query_Count { get; set; }

        /// <summary>
        /// Don't get data from before this data
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// Dont get data after this data
        /// </summary>
        public DateTime EndTimeUtc { get; set; }

        /// <summary>
        /// Comma-separated list of properties to sort by 
        /// </summary>
        [FunctionParameter(SourcePropertyName = "$OrderBy")]
        public string[] Query_OrderBy { get; set; }

        /// <summary>
        /// Filter statement 
        /// </summary>
        public string Filter { get; set; }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //--------------------------------------------------------------------------------
        public StandardRestQueryParameters()
        {
            Query_Top = 100;
            Query_Skip = 0;
            Query_Count = false;
            StartTimeUtc = DateTime.UtcNow.AddDays(-30);
            EndTimeUtc = DateTime.UtcNow;
        }
    }

}

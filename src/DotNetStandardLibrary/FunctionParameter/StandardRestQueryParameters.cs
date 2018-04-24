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
        /// Translates to $top.  (Which record to start returning) 
        /// </summary>
        [FunctionParameter(FixPropertyName = "Query_,$" )]
        public int Query_Top { get; set; }

        /// <summary>
        /// Translates to $skip.   (Size of the page in records)
        /// </summary>
        [FunctionParameter(FixPropertyName = "Query_,$")]
        public int Query_Skip { get; set; }

        /// <summary>
        /// Translates to $count.  (number of records to return)
        /// </summary>
        [FunctionParameter(FixPropertyName = "Query_,$")]
        public int Query_Count { get; set; }

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
        [FunctionParameter(FixPropertyName = "Query_,$")]
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
            Query_Top = 0;
            Query_Skip = 0;
            Query_Count = 100;
            StartTimeUtc = DateTime.UtcNow.AddDays(-30);
            EndTimeUtc = DateTime.UtcNow;
        }
    }

}

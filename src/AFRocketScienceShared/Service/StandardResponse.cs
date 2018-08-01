using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //------------------------------------------------------------------------------
    /// <summary>
    /// All responses from the service look like this, both data and errors.  This
    /// simplifies the receiving structures on the caller side. 
    /// </summary>
    //------------------------------------------------------------------------------
    public class ServiceResponse
    {
        /// <summary>
        /// Number of Values returned
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Error code - 0 == no error
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// The data returned
        /// </summary>
        public object[] Values { get; set; }

        /// <summary>
        /// Error messages (if any)
        /// </summary>
        public string ErrorMessage { get; set; }

        //------------------------------------------------------------------------------
        /// <summary>
        /// ctor - use any kind of data, this will stash it accordingly
        /// </summary>
        //------------------------------------------------------------------------------
        public ServiceResponse(object data)
        {
            Values = GetObjectArrayFromObject(data);
            Count = Values.Length;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Convert an object into an object array.  
        /// </summary>
        //------------------------------------------------------------------------------
        object[] GetObjectArrayFromObject(object data)
        {
            var arrayOutput = new object[0];
            if (data != null)
            {
                var dataType = data.GetType();
                if (typeof(IEnumerable<object>).IsAssignableFrom(data.GetType()))
                {
                    arrayOutput = ((IEnumerable<object>)data).ToArray();
                }
                else if (dataType.IsArray)
                {
                    throw new ArgumentException("Arrays of value types must be boxed as object arrays first.");
                }
                else
                {
                    arrayOutput = new object[] { data };
                }
            }

            return arrayOutput;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //------------------------------------------------------------------------------
    /// <summary>
    /// An exception peculiar service infrastructure.  Throw this kind of exception
    /// for normal kinds of errors that a caller would be expected to address.
    /// </summary>
    //------------------------------------------------------------------------------
    public class ServiceOperationException : System.Exception
    {
        public ServiceOperationError ErrorCode { get; set; }

        public ServiceOperationException(ServiceOperationError errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

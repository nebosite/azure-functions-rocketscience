using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Common functionality for controller handlers
    /// </summary>
    //--------------------------------------------------------------------------------
    public class ControllerHandlerBase
    {
        //--------------------------------------------------------------------------------
        /// <summary>
        /// Condense the call stack to a single file name and line number 
        /// </summary>
        //--------------------------------------------------------------------------------
        static string GetExceptionHint(Exception e)
        {
            var stackTrace = e.StackTrace;
            if (stackTrace == null) stackTrace = "";
            var match = Regex.Match(stackTrace, @"\\([^\\]*:line [0-9]+)");
            var firstLineInfo = match.Success ? $" ({match.Groups[1].Value})" : " (Unknown location)";

            return $"Debug hint: {e.Message}{firstLineInfo}";
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Framework for handling background jobs. 
        /// </summary>
        //--------------------------------------------------------------------------------
        public void SafelyTryJob(IServiceLogger logger, Action tryme)
        {
            try
            {
                tryme();
            }
            catch (Exception e)
            {
                var logKey = CurrentLogKey;
                logger.Error($"{logKey} Fatal Error: {e.ToString()}", e);
            }
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Framework for handling service calls so that the logging and return structure
        /// is regular. 
        /// </summary>
        //--------------------------------------------------------------------------------
        // TODO: replace TraceWriter with ILogger based logging service for easy testing.
        public HttpResponseMessage SafelyTry(IServiceLogger logger, Func<object> tryme)
        {
            try
            {
                var returnValue = tryme();
                if (returnValue is HttpResponseMessage) return returnValue as HttpResponseMessage;
                else return Ok(returnValue);
            }
            catch(Exception e)
            {
                return Error(e, logger);
            }
        }

        static string CurrentLogKey => $"LK[{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_ffffff")}]";

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Ok response 
        /// </summary>
        //--------------------------------------------------------------------------------
        public HttpResponseMessage Ok(object output = null)
        {
            var response = new ServiceResponse(output);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(response, Formatting.Indented), Encoding.UTF8, "application/json")
            };
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Error response 
        /// </summary>
        //--------------------------------------------------------------------------------
        public HttpResponseMessage Error(Exception error, IServiceLogger logger)
        {
            var logKey = CurrentLogKey;
            logger.Error($"{logKey} Service Error: {error.Message}", error);

            var statusCode = HttpStatusCode.BadRequest;
            var response = new ServiceResponse(null);
            if (error is ServiceOperationException)
            {
                var serviceError = error as ServiceOperationException;
                response.ErrorCode = serviceError.ErrorCode.ToString();
                response.ErrorMessage = serviceError.Message + $"\r\nThe Log Key for this error is {logKey}";
            }
            else
            {
                response.ErrorCode = ServiceOperationError.FatalError.ToString();
                statusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessage = $"There was a fatal service error.\r\nThe Log Key for this error is {logKey}\r\n{GetExceptionHint(error)}";
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonConvert.SerializeObject(response, Formatting.Indented), Encoding.UTF8, "application/json")
            };
        }
    }
}

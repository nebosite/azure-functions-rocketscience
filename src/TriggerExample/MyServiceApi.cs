using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.AFRocketScience;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TriggerExampleServiceLibrary;

namespace TriggerExample
{
    public static class MyServiceApi
    {
        /// <summary>
        /// A public static property called "Handler" is required.  This is where RocketScience looks
        /// for the code to handle the functions in this class.  
        /// </summary>
        public static MyHandler Handler { get; set; } = new MyHandler();

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Here is a simple http trigger API
        /// </summary>
        //--------------------------------------------------------------------------------
        [FunctionName("GetStuff")]
        public static async Task<HttpResponseMessage> GetStuff(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/stuff")]
            HttpRequestMessage req,
            ILogger log)
        {
            // Launchpad will automatically call Handler.GetStuff - it always looks
            // for a method on the handler with the same name as this one.
            return await LaunchPad.ExecuteHttpTrigger(req, log);
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Here is a simple http trigger API
        /// </summary>
        //--------------------------------------------------------------------------------
        [FunctionName("PostStuff")]
        public static async Task<HttpResponseMessage> PostStuff(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/stuff")]
            HttpRequestMessage req,
            ILogger log)
        {
            // Launchpad will automatically call Handler.GetStuff - it always looks
            // for a method on the handler with the same name as this one.
            return await LaunchPad.ExecuteHttpTrigger(req, log);
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// This is how you get swagger docs for free!  Note that the access on this 
        /// function is anonymous so people don't have to use a code to access the docs.
        /// </summary>
        //--------------------------------------------------------------------------------
        [FunctionName("Docs")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/docs")]
            HttpRequest req,
            ILogger log)
        {
            return LaunchPad.ShowSwaggerHtmlResponse(req, log);
        }
    }
}

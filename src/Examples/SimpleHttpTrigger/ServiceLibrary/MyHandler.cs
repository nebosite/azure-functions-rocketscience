using Microsoft.Azure.Functions.AFRocketScience;
using Swagger.ObjectModel;
using System;
using System.Linq;

namespace ServiceLibrary
{
    public class MyHandler
    {
        //------------------------------------------------------------------------------
        /// <summary>
        /// The arguments are always in a propertybag like this.   You can use the 
        /// FunctionParameter attribute to control many aspects of the properties.
        /// You can also inherit from other classes and those properties will be considered
        /// too.
        /// </summary>
        //------------------------------------------------------------------------------
        public class HelloArguments
        {
            /// <summary>
            /// Here is a required parameter with a special description for the docs
            /// 
            /// Note:  the FunctionParameter attribute is not required.  If it is missing, then 
            /// all defaults are assumed.
            /// </summary>
            [FunctionParameter(IsRequired = true, SwaggerDescription = "UserId from the Funcorp Database")]
            public int UserId { get; set; } = -1;  // Default values can be set inline

            /// <summary>
            /// this 'code' proeprty is necessary because the application will give back an error
            /// for every parameter it does not know about.   Since the code parameter 
            /// is required for authorization on the azure server, we include it here.   Parameters are 
            /// case insensitive.
            /// </summary>
            [FunctionParameter(SwaggerSecurityParameter = true)]
            public string Code { get; set; }

            /// <summary>
            /// Here is an array of strings that come from the header.  Arrays are comma-delimeted.
            /// </summary>
            [FunctionParameter(Source = ParameterIn.Header)]
            public string[] FishTypes { get; set; }

            public class Salutation
            {
                public string Text { get; set; }
                public int Value { get; set; }
            }

            /// <summary>
            /// One parameter can be fully constructed from the body, assuming the body is JSON text
            /// </summary>
            [FunctionParameter(Source = ParameterIn.Body)]
            public Salutation[] Salutations { get; set; }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// The handler for the Azure function "GetStuff"
        /// </summary>
        //------------------------------------------------------------------------------
        public object GetStuff(HelloArguments args, IServiceLogger log)
        {
            // The signature of the handler method is important.  First argument should be
            // your own special argument class that RocketScience will fill out for you.
            // The Second argument must be an IServiceLogger.  

            // The return type can be anything, even void.   RocketScience will automatically package
            // it in the "Values" section of the return JSON.
            // Except when:  If the function returns an HttpResponse, this with bypass the rocketscience
            // package logic and just return the response the function created.

            // THROWING EXCEPTIONS:
            // If you want to throw an exception: throw a ServiceOperationException and include a message
            // that the user can understand how to address the problem.  
            // If the exception is something the user can't fix, then throw any other kind of exception and 
            // RocketScience will automatically generate a genertic error message with a log key.
            if (args.UserId == 8888)
            {
                throw new ServiceOperationException(
                    ServiceOperationError.BadParameter,
                    "Please use a less auspicious UserId");
            }

            // You can return anything you want.  RocketScience will automatically package it
            // in the "Values" property on the return object. 
            return new 
            {
                Description = "Something old, something new" + 
                    (args.FishTypes == null ? "" : string.Join("", args.FishTypes.Select(a => ", something " + a))),
                AwesomeFactor = 1000 * args.UserId,
                StuffFromBody = args.Salutations
            };
        }

    }


}

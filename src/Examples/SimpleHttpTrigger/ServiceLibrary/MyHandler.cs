using Microsoft.Azure.Functions.AFRocketScience;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceLibrary
{
    public class MyHandler
    {
        //------------------------------------------------------------------------------
        /// <summary>
        /// Argument classes can inherit common argument from a base class like this
        /// </summary>
        //------------------------------------------------------------------------------
        public class CommonArguments
        {
            /// <summary>
            /// this 'code' proeprty is necessary because the application will give back an error
            /// for every parameter it does not know about.   Since the code parameter 
            /// is required for authorization on the azure server, we include it here.   Parameters are 
            /// case insensitive.
            /// </summary>
            [FunctionParameter(SwaggerSecurityParameter = true)]
            public string Code { get; set; }

            /// <summary>
            /// This is an easy way to get the bearer token.  Note that the name of the property 
            /// can be different than the name of the source (normally they are assumed to be the same)
            /// </summary>
            [FunctionParameter(Source = ParameterIn.Header, RemoveRequiredPrefix = "Bearer ", SourcePropertyName = "Authorization", IsRequired = true)]
            public string BearerToken { get; set; }

        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// The arguments are always in a propertybag like this.   You can use the 
        /// FunctionParameter attribute to control many aspects of the properties.
        /// You can also inherit from other classes and those properties will be considered
        /// too.
        /// </summary>
        //------------------------------------------------------------------------------
        public class GetStuffArguments : CommonArguments
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
            /// Here is an array of strings that come from the header.  Arrays are comma-delimeted.
            /// </summary>
            [FunctionParameter(Source = ParameterIn.Header)]
            public string[] FishTypes { get; set; }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// A helper method for checking the bearer token
        /// </summary>
        //------------------------------------------------------------------------------
        void Authorize(CommonArguments args)
        {
            // Naturally, you will put your own real authorization code here.  
            if(args.BearerToken != "funbucket")
            {
                throw new ServiceOperationException(ServiceOperationError.AuthorizationError, "The bearer token is supposed to be 'funbucket'");
            }
        }

        /// <summary>
        /// An example of something you might have on your server
        /// </summary>
        public class Stuff
        {
            public string Description { get; set; }
            public int AwesomeFactor { get; set; }
        }
        
        //------------------------------------------------------------------------------
        /// <summary>
        /// The handler for the Azure function "GetStuff"
        /// </summary>
        //------------------------------------------------------------------------------
        public Stuff GetStuff(GetStuffArguments args, IServiceLogger log)
        {
            // The signature of the handler method is important.  First argument should be
            // your own special argument class that RocketScience will fill out for you.
            // The Second argument must be an IServiceLogger.  

            // The return type can be anything, even void.   RocketScience will automatically package
            // it in the "Values" section of the return JSON.
            // Except when:  If the function returns an HttpResponse, this with bypass the rocketscience
            // package logic and just return the response the function created.

            // If you want to check the bearer token, do it at the start of your method
            Authorize(args);

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
            return new Stuff()
            {
                Description = "Something old, something new" +
                    (args.FishTypes == null ? "" : string.Join("", args.FishTypes.Select(a => ", something " + a))),
                AwesomeFactor = 1000 * args.UserId,
            };
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// This argument propertybag gets from the body, which is usually how POST
        /// calls work.  The body is expected to be JSON formatted
        /// </summary>
        //------------------------------------------------------------------------------
        public class PostStuffArguments : CommonArguments
        {
            public class StuffDescription
            {
                /// <summary>
                /// Some text
                /// </summary>
                [FunctionParameter(IsRequired = true)]
                public string Contents { get; set; }

                /// <summary>
                /// A numeric value
                /// </summary>
                public int HappinessPoints { get; set; }
            }

            /// <summary>
            /// One parameter can be fully constructed from the body, assuming the body is JSON text
            /// </summary>
            [FunctionParameter(Source = ParameterIn.Body, IsRequired = true)]
            public StuffDescription[] StuffItems { get; set; }
        }


        //------------------------------------------------------------------------------
        /// <summary>
        /// The handler for the Azure function "GetStuff"
        /// </summary>
        //------------------------------------------------------------------------------
        public bool PostStuff(PostStuffArguments args, IServiceLogger log)
        {
            // Again, make sure to check the bearer token first
            Authorize(args);

            // TODO: Actually store something in your database

            // Again we can return anything we want and rocketscience will package
            // it correctly.  In this case the first item in "Values" on the return
            // will be a boolean.
            return args.StuffItems.Length < 2;
        }
    }


}

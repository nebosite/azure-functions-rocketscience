
# azure-functions-rocketscience

A framework for simplifying azure functions.

### Current Features

* Code isolation from Azure Functions implementation
* Automatic handling of Errors, Arguments, and return packaging 
* Swagger automatic document generation
* Helpful logging and error messages

### Soon to be features:

* (Let me know what you want)
* Better Swagger output

# How to build a simple http service:
(Check out a simple example [here](https://github.com/nebosite/azure-functions-rocketscience/tree/master/src/Examples/SimpleHttpTrigger).)

1. Create an Azure Functions project in Visual Studio with default settings. 
    * This project will be used to do the following:
        * Define the external API
        * Construct objects needed to access azure resources
2. Add a C# class library (.Net Core) to your solution.
    * (Make sure the .net core version matches the version in the Azure Function project.)
    * This project will be used to implement business logic for your service
3. In the class library, create a public class with a name that makes sense. E.g. MyHandler
    * Add a dependency on the nuget package RocketScience.Azure.Functions
4. Hook up the Azure Function project to your logic
    * Add a dependency on the nuget package RocketScience.Azure.Functions
    * Add a public static property "Handler" of the same type as the class you created in the class library, and create an instance.  E.g.: 
    ```public static MyHandler Handler {get; set;} = new MyHander();```
    (_Note:  The public Handler property is how RocketScience finds your logic handler._)
5. Add a method to your API
    * Add an HttpTrigger method to the Function App's api class. Signature should be:
    ```public static async Task<HttpResponseMessage>```
    * Add this code as the method body:
    ```return await LaunchPad.ExecuteHttpTrigger(req, log);```
    (_Note: RocketScience tries to find a method with the same name as the caller.  Eg: if your trigger method is called GetFooBots, then RocketScience will try to call MyHandler.GetFooBots_)
    * In your handler class, add a method with the same name as your http trigger method.
        * This handler should have at least two paramaters:
            1. An argument class that defines how data makes it into your method (see the example code for more details)
            2. An ILogger object so you can log stuff
            3. (You can add additional parameters, but make sure to add them to the ExecuteHttpTrigger call.)
        * No need to do exception handling - rocketscience neatly handles all exceptions in a smart, clean way
        * Throw ServiceOperationException if you want the user to see caller to see details about what went wrong.  (Any other exception type will generate a fatal service error.  Rocketscience will report an error code that can be searched in the logs.)
        * Return any kind of object you want- RocketScience will package it correctly in a standard response.  The response has these properties:
            * Count: number of items in Values
            * ErrorCode: If there is an error, this will have a short text value for it.
            * Values: An array of any object your logic returned.
            * ErrorMessage: If there is an error, this will have a human-readable error message.
            * Return an HttpResponse if you want to bypass RocketScience return packaging
6. Add an API to retrieve swagger docs:
    * Add an Http "get" trigger
    * Use this body: 
    ```return LaunchPad.ShowSwaggerHtmlResponse(req, log);```
            
Your Function App class will look something like this:   

    public static class MyServiceApi
    {
        public static MyHandler Handler { get; set; } = new MyHandler();

        [FunctionName("GetStuff")]
        public static async Task<HttpResponseMessage> GetStuff(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/stuff")]HttpRequestMessage req, ILogger log)
        {
            return await LaunchPad.ExecuteHttpTrigger(req, log);
        }

        [FunctionName("Docs")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/docs")]HttpRequestMessage req, ILogger log)
        {
            return LaunchPad.ShowSwaggerHtmlResponse(req, log);
        }
    }

Your logic handler will look something like this: 

    public class MyHandler
    {
        public class GetStuffArguments
        {
            public bool IsOver9000 { get; set; } 
            public string UserFoo {get; set;}
        }
    
        public string[] GetStuff(GetStuffArguments args, ILogger log)
        {
            // your logic here...
        }
    }

### Best Practices
- Isolation: Access Keyvault, data, and other platform specific features through interfaces
        - Define the interface in the class library
        - implement the interface in the Azure Functions project

	

# Using the keyvault
(obsolete.  I have left the old keyvault code commented out if you want to pull it into your own code and use it.)
1. Your app needs a registered app identity which you create inside Azure portal.
1. Open App registrations in Azure Portal
1. Click 'New application registration'
1. Create your own self-signed certificate
1. Instructions later
1. Export public key
1. Attach the cert to the registered app identity
1. Open App registrations in Azure Portal
1. Click on your app registration
1. Go to Settings-> Keys-> Upload Public Key
1. Upload your cert public key
1. Create a KeyVault
1. Add your app Id as an authorized key vault user
1. Add secrets to you keyvault


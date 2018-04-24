
# azure-functions-rocketscience

A framework for simplifying azure functions.

### Current Features

* Very simple setup of full-featured Azure functions, allowing you to focus on your work instead of setup.
* Automatic HTTP request parsing through your own custom parameter class.  Generates helpful error messages with input parameters are wrong.
* Automatic generation of Swagger documentation 
* Smart error handling
* Simplified Azure keyvault access

### Soon to be features:

* Support for data binding
* Better support for timer triggers
* Better Swagger output UI


# How to use:
(A very simple example can be found [here](https://github.com/nebosite/azure-functions-rocketscience/tree/master/src/Examples/SimpleHttpTrigger).)

1. Using visual studio, create two projects:  
	a.  An Azure functions project to expose your endpoints
	b. A Class library for your implementation code.
2. Link your projects against The [AFRocketScience Nuget package](https://www.nuget.org/packages/RocketScience.Azure.Functions/)
3. Create your handler code in the class library project
	a. First argument is a custom type that has parameters you care about.
	b. Second argument is an ISessionLogger
	c. Don't worry about error handling.  Just write code to do the job.  RocketScience will handle the details!
4. Create an Azure endpoint.
	a. Add a Handler property to expose your handler class
	b. Call LaunchPad.ExecuteHttpTrigger() - RocketScience with automatically pull parameters out of the request and call your handler with them.
	
Roughly:
  

    public class MyHandler
    {
        public class MyArguments
        {
            public bool IsOver9000 { get; set; } 
            public string FlubberBus {get; set;}
            public string[] FishTypes { get; set; }
        }
    
        public string[] GetStuff(MyArguments args, IServiceLogger log)
        {
            return new [] { args.FishTypes }
        }
    }
    
    public static class MyServiceApi
    {
        public static MyHandler Handler { get; set; } = new MyHandler();
    
        [FunctionName("GetStuff")]
        public static async Task<HttpResponseMessage> GetStuff(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/getstuff")]
            HttpRequestMessage req, 
            TraceWriter log)
        {
            return await LaunchPad.ExecuteHttpTrigger(req, new TraceLogger(log));
        }
    }

# Using the keyvault

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


# azure-functions-rocketscience
A framework for simplifying azure functions.  

###Current Features
* Automatic packaging of output into uniform JSON
* Smart error handling with log keys
* Automatic HTTPRequest parameter parsing using a template class
* SImplified keyvault access

###Soon to be features:
* Automatic Swagger documentation generation


# How to use:
1. Create your app in Visual Studio.  You will need two projects: 
  1. A standard VS Azure Function app (this will be a small project just to expose the functions)
  1. A C# class library (This will contain 95% of the code.)  Why:
    * Unit test code does not play well with Azure function projects
    * Creating a separate projects isolates the main code from the dll-hell that is azure functions

1. Link your class library against The AFRocketScience Nuget package

1. Create an Azure endpoint 

  * HTTP Trigger example: 
    1. Add a new HTTP trigger to your Azure functions app
    1. Create a class in the library to hold the function logic
    
        public class MyFooHandler : BaseHandler
        {
            public async Task<HttpResponseMessage> GetFoo(
                HttpRequestMessage req, 
                IServiceLogger log)
            {
                return SafelyTry(log, () =>
                {
                    // Your logic here
                    return whateverYouWant;
                });
            }
        }
    
    1. Create a lazy singleton instance in the Azure function to your class
    
        private static readonly Lazy<MyFooHandler> _lazyHandler = new Lazy<MyFooHandler>(() => new MyFooHandler());
        public static MyFooHandler Handler => _lazyHandler.Value;

    1. Call your method from the Azure function:
    
        return await Handler.GetFoo(req, new MyLogImplementation(log));

  * Timer Trigger example: 
    1. Add a Timer trigger to your Azure functions app
    1. Create a class in the library to hold the function logic
    
        public class MyLogicClass : BaseHandler
        {
            public async Task<HttpResponseMessage> PackageTheWeebles(
                IServiceLogger log)
            {
                return SafelyTryJob(log, () =>
                {
                    // Your logic here
                });
            }
        }
    
    1. Create a lazy singleton instance in the Azure function to your class
    
        private static readonly Lazy<MyLogicClass> _lazyHandler = new Lazy<MyLogicClass>(() => new MyLogicClass());
        public static MyLogicClass Handler => _lazyHandler.Value;

    1. Call your method from the Azure function:
    
        return await Handler.PackageTheWeebles(new MyLogImplementation(log));


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
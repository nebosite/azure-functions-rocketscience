using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// The Launchpad is the connection between the Azure function and your handler.
    /// Call LaunchPad.Go to execute your handler code automatically.
    /// </summary>
    //--------------------------------------------------------------------------------
    public class LaunchPad
    {
        /// <summary>
        /// Launchpad is single instanced
        /// </summary>
        private static readonly Lazy<LaunchPad> _lazyHandler = new Lazy<LaunchPad>(() => new LaunchPad());
        public static LaunchPad Instance => _lazyHandler.Value;
        public static SwaggerRoot _docs;

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Shortcut to determin the azure funtion that ultimately called us
        /// </summary>
        //--------------------------------------------------------------------------------
        static MethodBase GetCallingAzureFunction()
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            for(int i = 0; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                var functionAtttribute = GetCustomAttribute(method, "FunctionNameAttribute");
                if (functionAtttribute != null) return method;
            }
            return null;
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Get the swagger documentation for this assembly
        /// </summary>
        //--------------------------------------------------------------------------------
        public static HttpResponseMessage ShowSwaggerHtmlResponse(HttpRequestMessage req, ILogger logger)
        {
            return ShowSwaggerHtmlResponse(new RSHttpRequestMessage(req), logger);
        }
        public static HttpResponseMessage ShowSwaggerHtmlResponse(HttpRequest req, ILogger logger)
        {
            return ShowSwaggerHtmlResponse(new RSHttpRequest(req), logger);
        }
        public static HttpResponseMessage ShowSwaggerHtmlResponse(IRocketScienceRequest req, ILogger logger)
        {
            try
            {
                if (_docs == null)
                {
                    try
                    {
                        _docs = GenerateSwagger(req, logger, GetCallingAzureFunction().DeclaringType.Assembly);
                    }
                    catch (Exception e)
                    {
                        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                        errorResponse.Content = new StringContent($"Docs Error: {e.Message}");
                        errorResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                        return errorResponse;
                    }
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(GetSwaggerHtml(_docs));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;

            }
            catch(Exception e)
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                response.Content = new StringContent($"<pre>Whoops: \r\n{e.ToString()}</pre>");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Turn swagger documentation into html
        /// </summary>
        //--------------------------------------------------------------------------------
        private static string GetSwaggerHtml(SwaggerRoot docs)
        {
            var content = new StringBuilder();
            content.AppendLine("<pre>");
            content.AppendLine($"{docs.Info.Title} Version {docs.Info.Version}");
            content.AppendLine($"\r\nPaths: ");

            void DoOperation(string path, Operation operation)
            {
                if (operation == null) return;
                content.AppendLine($"    {docs.BasePath}{path}");
                content.AppendLine($"        Parameters:");
                foreach(var parameter in operation.Parameters)
                {
                    var requiredText = parameter.Required.Value ? " * " : "   ";
                    content.AppendLine($"            {requiredText}{parameter.Type} {parameter.Name} in {parameter.In}   {parameter.Description}");
                }

                content.AppendLine($"        Responses:");
                foreach (var response in operation.Responses)
                {
                    content.AppendLine($"               {response.Key} {response.Value.Schema.Type} {response.Value.Description}");
                    foreach(var item in response.Value.Schema.Properties)
                    {
                        content.AppendLine($"                   {item.Value.Type} {item.Key} {item.Value.Description}");
                    }
                }

            }

            foreach (var pathEntry in docs.Paths)
            {
                DoOperation(pathEntry.Key, pathEntry.Value.Get);
                DoOperation(pathEntry.Key, pathEntry.Value.Delete);
                DoOperation(pathEntry.Key, pathEntry.Value.Head);
                DoOperation(pathEntry.Key, pathEntry.Value.Options);
                DoOperation(pathEntry.Key, pathEntry.Value.Patch);
                DoOperation(pathEntry.Key, pathEntry.Value.Post);
                DoOperation(pathEntry.Key, pathEntry.Value.Put);
            }

            content.AppendLine($"Security:");

            foreach (var securityEntry in docs.SecurityDefinitions)
            {
                content.AppendLine($"     {securityEntry.Key}:{securityEntry.Value.Name}");
            }
            content.AppendLine("</pre>");
            return content.ToString();
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Shortcut to get property values when we don't have access to the type
        /// </summary>
        //--------------------------------------------------------------------------------
        static T GetPropertyValue<T>(object source, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            if (property == null) throw new ArgumentException($"Source object does not have property '{propertyName}'");

            return (T)(property.GetValue(source));
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Shortcut to get a specified custom attribute
        /// </summary>
        //--------------------------------------------------------------------------------
        static object GetCustomAttribute(ICustomAttributeProvider item, string attributeName)
        {
            return item.GetCustomAttributes(true).Where(a => a.GetType().Name == attributeName).FirstOrDefault();
        }
        static object GetCustomAttribute(ICustomAttributeProvider item, Type attributeType)
        {
            return GetCustomAttribute(item, attributeType.Name);
        }


        //--------------------------------------------------------------------------------
        /// <summary>
        /// Auto-generate swagger doc tree from the calling assembly
        /// </summary>
        //--------------------------------------------------------------------------------
        private static SwaggerRoot GenerateSwagger(IRocketScienceRequest req, ILogger logger, Assembly functionAssembly)
        {
            var caller = GetCallingAzureFunction();
            var paths = new Dictionary<string, PathItem>();


            string GetKeyFromTrigger(object httpTriggerAttribute)
            {
                return GetPropertyValue<string>(httpTriggerAttribute, "Route") 
                    + " (" 
                    + string.Join(",", GetPropertyValue<string[]>(httpTriggerAttribute, "Methods")) + ")";
            }

            // Go through all the types in the calling assembly
            foreach (var type in functionAssembly.GetTypes())
            {
                // Azure functions are public and static
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    // Must have a function name attribute
                    var functionAtttribute = method.CustomAttributes.Where(a => a.AttributeType.Name == "FunctionNameAttribute").FirstOrDefault();
                    if (functionAtttribute == null) continue;

                    // Must have http trigger attribute on the first parameter
                    var parameters = method.GetParameters();
                    if (parameters.Length < 2) continue;
                    var httpTriggerAttribute = GetCustomAttribute(parameters[0], "HttpTriggerAttribute");
                    if (httpTriggerAttribute == null) continue;

                    // By convention, we'll exclude the azure function calling us
                    if (method.Name == caller.Name && method.DeclaringType.Name == caller.DeclaringType.Name) continue;

                    paths.Add(GetKeyFromTrigger(httpTriggerAttribute), CreatePathItemFromMethod(method));
                }
            }

            var security = new Dictionary<string, SecurityScheme>();
            security.Add("apikeyQuery", new SecurityScheme()
            {
                Name = "code",
                Type = SecuritySchemes.ApiKey,
                In = ApiKeyLocations.Query
            });

            var callerTriggerInfo = GetCustomAttribute(caller.GetParameters()[0], "HttpTriggerAttribute");
            var callerPath = GetPropertyValue<string>(callerTriggerInfo, "Route");

            var root = new SwaggerRoot()
            {
                Info = new Info() { Title = req.Host, Version = functionAssembly.GetName().Version.ToString() },
                Host = req.Host,
                BasePath = req.LocalPath.Substring(0, req.LocalPath.Length - callerPath.Length),
                Schemes = new[] { Schemes.Https },
                Paths = paths,
                Definitions = new Dictionary<string, Schema>(),
                SecurityDefinitions = security
            };

            return root;
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Auto-generate swagger PathItem from an http trigger method
        /// </summary>
        //--------------------------------------------------------------------------------
        private static PathItem CreatePathItemFromMethod(MethodInfo azureFunction)
        {
            var functionNameAttribute = GetCustomAttribute(azureFunction, "FunctionNameAttribute");
            var httpTriggerAttribute = GetCustomAttribute(azureFunction.GetParameters()[0], "HttpTriggerAttribute");
            
            var handlerProperty = azureFunction.DeclaringType.GetProperty("Handler", BindingFlags.Public | BindingFlags.Static);
            var handlerMethod = handlerProperty.PropertyType.GetMethod(azureFunction.Name);

            if(handlerMethod == null)
            {
                throw new ApplicationException($"No handler method was found for {azureFunction.Name} ({string.Join(",", GetPropertyValue<string[]>(httpTriggerAttribute, "Methods"))})");
            }

            var parameters = new  List<Parameter>();
            foreach(var property in handlerMethod.GetParameters()[0].ParameterType.GetProperties())
            {
                var functionInfo = property.GetRocketScienceAttribute();
                var name = property.GetSourcePropertyName();

                parameters.Add(new Parameter()
                {
                    Name = name,
                    In = functionInfo == null ? ParameterIn.Query : functionInfo.Source,
                    Description = functionInfo?.SwaggerDescription,
                    Required = functionInfo == null ? false : functionInfo.IsRequired,
                    Type = property.PropertyType.Name,
                });
            }

            var schemaProperties = new Dictionary<string, Schema>();
            schemaProperties.Add("Count", new Schema() { Type = "integer", Description = "Number of Values" });
            schemaProperties.Add("ErrorCode", new Schema() { Type = "string", Description = "Short Text Description of any error (if any)" });
            schemaProperties.Add("ErrorMessage", new Schema() { Type = "string", Description = "Detailed error message (if any)" });
            schemaProperties.Add("Value", new Schema() { Type = "array", Items = new Item() { Type = handlerMethod.ReturnType.Name }, Description = "Data returned from a successful operation (in any)" });
            var itemResponses = new Dictionary<string, Response>();
            itemResponses.Add("*", new Response()
            {
                Description = "Any Response",
                Schema = new Schema()
                {
                    Type = "object",
                    Properties = schemaProperties
                }
            });


            var pathItem = new PathItem();
            var operation  = new Operation()
                {
                    OperationId = GetPropertyValue<string>(functionNameAttribute, "Name"),
                    Produces = new[] { "application/json" },
                    Consumes = new[] { "application/json" },
                    Parameters = parameters,
                    Responses = itemResponses
            };

            foreach(var method in GetPropertyValue<string[]>(httpTriggerAttribute, "Methods"))
            {
                switch(method.ToLower())
                {
                    case "get": pathItem.Get = operation; break;
                    case "post": pathItem.Post = operation; break;
                    case "put": pathItem.Put = operation; break;
                    case "head": pathItem.Head = operation; break;
                    case "patch": pathItem.Patch = operation; break;
                    case "delete": pathItem.Delete = operation; break;
                    case "options": pathItem.Options = operation; break;
                }
            }

            return pathItem;
        }

        static Dictionary<string, Vehicle> _vehicles = new Dictionary<string, Vehicle>();

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Automatically call the execution logic for the trigger that called us.
        /// function. 
        /// </summary>
        //--------------------------------------------------------------------------------
        public static async Task<HttpResponseMessage> ExecuteHttpTrigger(HttpRequestMessage req, ILogger logger, params object[] extras)
        {
            return await ExecuteHttpTrigger(new RSHttpRequestMessage(req), logger, extras);
        }
        public static async Task<HttpResponseMessage> ExecuteHttpTrigger(HttpRequest req, ILogger logger, params object[] extras)
        {
            return await ExecuteHttpTrigger(new RSHttpRequest(req), logger, extras);
        }
        public static async Task<HttpResponseMessage> ExecuteHttpTrigger(IRocketScienceRequest req, ILogger logger, params object[] extras)
        {
            var requestKey = req.Key;
            return Instance.SafelyTry(logger, () =>
            {
                if(!_vehicles.TryGetValue(requestKey, out var caller))
                {
                    var callingMethod = GetCallingAzureFunction();
                    var handlerProperty = callingMethod.DeclaringType.GetProperty("Handler", BindingFlags.Public | BindingFlags.Static);

                    if(handlerProperty == null)
                    {
                        throw new ApplicationException(
                            $"The type '{callingMethod.DeclaringType.Name}' does not have a public static property 'Handler'.  "
                            +  $"This is necessary for RocketScience to discover the object to handle service logic.");
                    }

                    caller = new Vehicle(callingMethod, handlerProperty);
                    _vehicles.Add(requestKey, caller);

                }
                return caller.ExecuteHttpRequest(req, logger, extras);
            });
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Condense the call stack to a single file name and line number 
        /// </summary>
        //--------------------------------------------------------------------------------
        static string GetExceptionHint(Exception e)
        {
#if DEBUG
            var stackTrace = e.StackTrace;
            if (stackTrace == null) stackTrace = "";
            var match = Regex.Match(stackTrace, @"\\([^\\]*:line [0-9]+)");
            var firstLineInfo = match.Success ? $" ({match.Groups[1].Value})" : " (Unknown location)";

            return $"Debug hint: {e.Message}{firstLineInfo}";
#else
            return "";
#endif
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Framework for handling background jobs. 
        /// </summary>
        //--------------------------------------------------------------------------------
        internal void SafelyTryJob(ILogger logger, Action tryme)
        {
            try
            {
                tryme();
            }
            catch (Exception e)
            {
                var logKey = CurrentLogKey;
                logger.LogError($"{logKey} Fatal Error: {e.ToString()}", e);
            }
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Framework for handling service calls so that the logging and return structure
        /// is regular. 
        /// </summary>
        //--------------------------------------------------------------------------------
        internal HttpResponseMessage SafelyTry(ILogger logger, Func<object> tryme)
        {
            try
            {
                var returnValue = tryme();
                if (returnValue is HttpResponseMessage) return returnValue as HttpResponseMessage;
                else return Ok(returnValue);
            }
            catch (Exception e)
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
        public static HttpResponseMessage Ok(object output = null)
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
        public static HttpResponseMessage Error(Exception error, ILogger logger)
        {
            if (error is TargetInvocationException) error = ((TargetInvocationException)error).InnerException;
            var logKey = CurrentLogKey;
            logger.LogError($"{logKey} Service Error: {error}", error);
           

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

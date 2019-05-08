using Newtonsoft.Json;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// The Vehicle represents a single API call template
    /// </summary>
    //--------------------------------------------------------------------------------
    public class Vehicle
    {
        PropertyInfo _handlerStaticProperty;
        MethodInfo _callMe;
        MethodInfo _constructParameters;
        ParameterDefinition[] _parameterDefinitions;

        class ParameterDefinition
        {
            public Func<HttpRequestMessage, IServiceLogger, object[], object> Create { get; set; }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //------------------------------------------------------------------------------
        public Vehicle(MethodBase method, PropertyInfo handlerStaticProperty)
        {
            _handlerStaticProperty = handlerStaticProperty;
            var handlerType = handlerStaticProperty.PropertyType;
            _callMe = handlerType.GetMethod(method.Name);
            if (_callMe == null) throw new ApplicationException($"The type '{handlerType.Name}' does not have a method '{method.Name}'");
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Execute this call as an Http request
        /// </summary>
        //------------------------------------------------------------------------------
        public object ExecuteHttpRequest(HttpRequestMessage req, IServiceLogger logger, params object[] extras)
        {
            var handler = _handlerStaticProperty.GetValue(null);
            var generatedParameters = new List<object>();

            if (_parameterDefinitions == null)
            {
                var newDefinitions = new List<ParameterDefinition>();
                var targetParameters = _callMe.GetParameters();
                if (extras == null) extras = new Tuple<string, object>[0];
                if (targetParameters.Length != 2 + extras.Length)
                {
                    throw new ApplicationException($"The target method '{_callMe.Name}' should have {2 + extras.Length} parameters, but has {targetParameters.Length}.");
                }

                // The first parameter is the user's custom property bag class which we will generically construct 
                // from the request using the ReadParameters method
                var firstParameterType = targetParameters[0].ParameterType;
                var context = this.GetType();
                var readParametersMethod = context.GetMethod("ReadParameters", BindingFlags.Static | BindingFlags.Public);
                _constructParameters = readParametersMethod.MakeGenericMethod(new[] { firstParameterType });
                newDefinitions.Add(new ParameterDefinition()
                {
                    Create = (r, l, e) => _constructParameters.Invoke(null, new object[] { r })
                });

                // The second parameter is just the passed in IserviceLogger
                if (targetParameters[1].ParameterType.Name != "IServiceLogger")
                {
                    throw new ApplicationException($"The target method '{_callMe.Name}' second parameter should be type IServiceLogger.");
                }
                newDefinitions.Add(new ParameterDefinition() { Create = (r, l, e) => l });

                // fill in any remaining parameters with the extra named arguments
                for (int i = 2; i < targetParameters.Length; i++)
                {
                    var index = i - 2;
                    newDefinitions.Add(new ParameterDefinition() { Create = (r, l, e) => e[index] });
                }

                _parameterDefinitions = newDefinitions.ToArray();
            }

            foreach (var parameter in _parameterDefinitions)
            {
                generatedParameters.Add(parameter.Create(req, logger, extras));
            }


            return _callMe.Invoke(handler, generatedParameters.ToArray());
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Yes, I know this is provided in system.web.http, but the point of this library
        /// is to avoid that dependency
        /// </summary>
        //------------------------------------------------------------------------------
        public static KeyValuePair<string, string>[] GetQueryNameValuePairs(HttpRequestMessage request)
        {
            var queryParts = request.RequestUri.Query.TrimStart('?').Split('&');
            var output = new List<KeyValuePair<string, string>>();
            foreach (var part in queryParts)
            {
                var trimmed = part.Trim();
                if (trimmed == "") continue;
                var subParts = trimmed.Split(new[] { '=' }, 2);
                output.Add(new KeyValuePair<string, string>(subParts[0], subParts.Length > 1 ? Uri.UnescapeDataString(subParts[1]).Trim() : null));
            }

            return output.ToArray();
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Generic way to handle parameters
        /// </summary>
        //------------------------------------------------------------------------------
        public static T ReadParameters<T>(HttpRequestMessage request) where T : new()
        {
            var queryProperties = new List<PropertyInfo>();
            var headerProperties = new List<PropertyInfo>();
            var requiredProperties = new List<PropertyInfo>();
            var bodyProperties = new List<PropertyInfo>();
            var uriPairs = new List<KeyValuePair<string, string>>();
            var errors = new List<string>();

            uriPairs.AddRange(GetQueryNameValuePairs(request));

            T output = ReadParameters<T>(request, uriPairs, queryProperties, headerProperties, requiredProperties, bodyProperties, errors);

            foreach(var uriParameter in uriPairs)
            {
                errors.Add($"Unknown URI parameter:  '{uriParameter.Key}'");
            }

            foreach (var property in requiredProperties)
            {
                errors.Add($"Missing required parameter '{property.GetSourcePropertyName()}'");
            }

            if (errors.Count > 0) throw new ServiceOperationException(ServiceOperationError.BadParameter, string.Join("\r\n", errors));
            return output;
        }
        //------------------------------------------------------------------------------
        /// <summary>
        /// Generic way to handle parameters
        /// </summary>
            //------------------------------------------------------------------------------
        private static T ReadParameters<T>(
            HttpRequestMessage request,
            List<KeyValuePair<string,string>> uriParameters,
            List<PropertyInfo> queryProperties,
            List<PropertyInfo> headerProperties,
            List<PropertyInfo> requiredProperties,
            List<PropertyInfo> bodyProperties,
            List<string> errors
            ) where T : new()
        {
            T output = new T();
            var headers = request.Headers;

            foreach (var property in output.GetType().GetProperties())
            {
                var parameterInfo = property.GetParams();
                if (parameterInfo.Ignore)
                {
                    continue;
                }

                if (property.PropertyType.IsClass 
                    && property.PropertyType.Name != "String"
                    && !property.PropertyType.IsArray)
                {
                    try
                    {
                        var readParamesMethod = typeof(Vehicle).GetMethod("ReadParameters", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(property.PropertyType);
                        property.SetValue(output, readParamesMethod.Invoke(null, new object[] { headers, uriParameters, queryProperties, headerProperties, requiredProperties, errors }));
                    }
                    catch (Exception e)
                    {
                        if (e is TargetInvocationException)
                        {
                            e = ((TargetInvocationException)e).InnerException;
                        }
                        errors.Add($"Error on property group '{property.Name}': {e.Message}");
                    }
                    continue;
                }

                if (parameterInfo.IsRequired)
                {
                    requiredProperties.Add(property);
                }

                switch (parameterInfo.Source)
                {
                    case ParameterIn.Query:
                        queryProperties.Add(property);
                        break;
                    case ParameterIn.Header:
                        headerProperties.Add(property);
                        break;
                    case ParameterIn.Body:
                        bodyProperties.Add(property);
                        break;
                    default:
                        errors.Add($"Error on ({property.PropertyType.Name}) property '{property.GetSourcePropertyName()}':  Can't get parameters from " + parameterInfo.Source);
                        break;
                }
            }

            // Common way to read a property. Returns true if there was a property name match
            bool DigestProperty(PropertyInfo property, string rawParameterName, string parameterValue, string prefix = null)
            {
                var propertyName = property.GetSourcePropertyName().ToLower();
                var parameterName = rawParameterName.ToLower();
                if (propertyName == parameterName)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            if (!parameterValue.StartsWith(prefix))
                            {
                                errors.Add($"Error on ({property.PropertyType.Name}) property '{property.GetSourcePropertyName()}': Required prefix '{prefix}' was missing.");
                                return true;
                            }
                            parameterValue = parameterValue.Substring(prefix.Length);
                        }

                        if (property.PropertyType.IsArray)
                        {
                            var parts = parameterValue.Split(',');
                            var elementType = property.PropertyType.GetElementType();
                            var array = Array.CreateInstance(elementType, parts.Length);
                            for (int i = 0; i < parts.Length; i++)
                            {
                                array.SetValue(ParseValue(elementType, parts[i]), i);
                            }
                            property.SetValue(output, array);
                        }
                        else
                        {
                            property.SetValue(output, ParseValue(property.PropertyType, parameterValue));
                        }
                    }
                    catch (Exception e)
                    {
                        if(e is TargetInvocationException)
                        {
                            e = ((TargetInvocationException)e).InnerException;
                        }
                        errors.Add($"Error on ({property.PropertyType.Name}) property '{property.GetSourcePropertyName()}': {e.Message}");
                    }
                    if (requiredProperties.Contains(property)) requiredProperties.Remove(property);
                    return true;
                }

                return false;
            }

            foreach (var uriParameter in uriParameters.ToArray())
            {
                foreach (var property in queryProperties)
                {
                    if (DigestProperty(property, uriParameter.Key, uriParameter.Value))
                    {
                        uriParameters.Remove(uriParameter);
                        break;
                    }
                }
            }

            foreach (var headerItem in headers)
            {
                var headerValues = headerItem.Value.ToArray();

                foreach (var property in headerProperties.ToArray())
                {
                    var parameterInfo = property.GetParams();

                    if (DigestProperty(property, headerItem.Key, headerValues[0], parameterInfo?.RemoveRequiredPrefix))
                    {
                        headerProperties.Remove(property);
                        break;
                    }
                }
            }

            if (bodyProperties.Count > 1)
            {
                errors.Add("There can be only parameter that comes from the body.  Body parameters: " + string.Join(",", bodyProperties.Select(bp => bp.Name)));
            }
            else if (bodyProperties.Count == 1)
            {
                var bodyText = request.Content.ReadAsStringAsync().Result;
                bodyProperties[0].SetValue(output, bodyText );
                if (requiredProperties.Contains(bodyProperties[0])) requiredProperties.Remove(bodyProperties[0]);
            }


            return output;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Turn a string value into an object of the right type
        /// </summary>
        //------------------------------------------------------------------------------
        private static object ParseValue(Type type, string value)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, value, true);
            }

            switch (type.Name)
            {
                case "String": return value.Trim();
                case "Guid": return Guid.Parse(value);
                case "Char": return Char.Parse(value);
                case "Byte": return Byte.Parse(value);
                case "Int16": return Int16.Parse(value);
                case "Int32": return Int32.Parse(value);
                case "Int64": return Int64.Parse(value);
                case "Double": return Double.Parse(value);
                case "DateTime": return DateTime.Parse(value);
                default: throw new ApplicationException($"Unknown type: {type.Name}");
            }
        }

    }
}

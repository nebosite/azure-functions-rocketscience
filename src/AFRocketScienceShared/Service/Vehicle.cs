﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
            public Func<IRocketScienceRequest, ILogger, object[], object> Create { get; set; }
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
        public object ExecuteHttpRequest(IRocketScienceRequest req, ILogger logger, params object[] extras)
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

                // The second parameter is just the passed in ILogger
                if (targetParameters[1].ParameterType.Name != nameof(ILogger))
                {
                    throw new ApplicationException($"The target method '{_callMe.Name}' second parameter should be type {nameof(ILogger)}.");
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
        /// Generic way to handle parameters
        /// </summary>
        //------------------------------------------------------------------------------
        public static T ReadParameters<T>(IRocketScienceRequest request) where T : new()
        {
            var queryProperties = new List<PropertyInfo>();
            var headerProperties = new List<PropertyInfo>();
            var requiredProperties = new List<PropertyInfo>();
            var bodyProperties = new List<PropertyInfo>();
            var uriPairs = new List<KeyValuePair<string, string>>();
            var errors = new List<string>();

            uriPairs.AddRange(request.QueryParts);

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
            IRocketScienceRequest request,
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
                var parameterInfo = property.GetRocketScienceAttribute();
                if (parameterInfo.Ignore)
                {
                    continue;
                }

                // If the property is a class that is not coming from the body,
                // then treat the properties properties as first class parameters
                if (property.PropertyType.IsClass 
                    && property.PropertyType.Name != "String"
                    && !property.PropertyType.IsArray
                    && parameterInfo.Source != ParameterIn.Body)
                {
                    try
                    {
                        var readParamesMethod = typeof(Vehicle).GetMethod("ReadParameters", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(property.PropertyType);

                        property.SetValue(output, readParamesMethod.Invoke(null, new object[] { request, uriParameters, queryProperties, headerProperties, requiredProperties, bodyProperties, errors }));
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
                        else if (property.PropertyType.IsClass &&  property.PropertyType.Name != "String")
                        {
                            property.SetValue(output, JsonConvert.DeserializeObject(parameterValue, property.PropertyType));
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
                foreach (var property in headerProperties.ToArray())
                {
                    var parameterInfo = property.GetRocketScienceAttribute();

                    if (DigestProperty(property, headerItem.Key, headerItem.Value, parameterInfo?.RemoveRequiredPrefix))
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
                if(request.Content == null)
                {
                    errors.Add("The POST body response is missing");
                }
                else
                {
                    var bodyText = request.Content;
                    try
                    {
                        var bodyValue = JsonConvert.DeserializeObject(bodyText, bodyProperties[0].PropertyType);
                        if(bodyValue != null)
                        {
                            bodyProperties[0].SetValue(output, bodyValue);
                            if (requiredProperties.Contains(bodyProperties[0])) requiredProperties.Remove(bodyProperties[0]);
                            FlagErrors(bodyValue, errors);
                        }
                    }
                    catch(Exception e)
                    {
                        errors.Add($"Could not create parameter '{bodyProperties[0].Name}' from the posted body because: {e.Message}");
                    }
                }
            }


            return output;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Look at the properties on this object and see if they follow the 
        /// function parameter requirements
        /// </summary>
        //------------------------------------------------------------------------------
        private static void FlagErrors(object bodyValue, List<string> errors)
        {
            var enumerableBody = bodyValue as IEnumerable<object>;
            if(enumerableBody != null)
            {
                int count = 0;
                foreach(var item in enumerableBody)
                {
                    count++;
                    FlagErrorsOnItem($"Item {count}: ", item, errors);
                }
            }
            else
            {
                FlagErrorsOnItem($"Item from body ", bodyValue, errors);
            }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Look at the properties on this object and see if they follow the 
        /// function parameter requirements
        /// </summary>
        //------------------------------------------------------------------------------
        private static void FlagErrorsOnItem(string prefix, object item, List<string> errors)
        {
            foreach(var property in item.GetType().GetProperties())
            {
                var attribute = property.GetRocketScienceAttribute();
                if(attribute.IsRequired && property.GetValue(item) == null)
                {
                    errors.Add($"{prefix}missing required parameter '{property.Name}'");
                }
            }
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

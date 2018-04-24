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
            public Func<HttpRequestMessage, IServiceLogger, object> Create { get; set; }
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
        public object ExecuteHttpRequest(HttpRequestMessage req, IServiceLogger logger)
        {
            var handler = _handlerStaticProperty.GetValue(null);
            var generatedParameters = new List<object>();

            if (_parameterDefinitions == null)
            {
                var newDefinitions = new List<ParameterDefinition>();
                var targetParameters = _callMe.GetParameters();
                if (targetParameters.Length < 2)
                {
                    throw new ApplicationException($"The target method '{_callMe.Name}' has too few parameters. It must have at least two: (MyArguments args, IServiceLogger logger, ...)");
                }

                var firstParameterType = targetParameters[0].ParameterType;
                if (firstParameterType.Name == "IServiceLogger")
                {
                    throw new ApplicationException($"The target method '{_callMe.Name}' first parameter should be your argument class type, not IServiceLogger.");
                }

                var context = this.GetType();
                var readParametersMethod = context.GetMethod("ReadParameters", BindingFlags.Static | BindingFlags.Public);
                _constructParameters = readParametersMethod.MakeGenericMethod(new[] { firstParameterType });

                newDefinitions.Add(new ParameterDefinition()
                {
                    Create = (r, l) =>
_constructParameters.Invoke(null, new object[] { r })
                });
                newDefinitions.Add(new ParameterDefinition() { Create = (r, l) => l });
                _parameterDefinitions = newDefinitions.ToArray();
            }

            foreach (var parameter in _parameterDefinitions)
            {
                generatedParameters.Add(parameter.Create(req, logger));
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
            T output = new T();
            var queryProperties = new List<PropertyInfo>();
            var headerProperties = new List<PropertyInfo>();
            var errors = new List<string>();

            var requiredProperties = new List<PropertyInfo>();
            foreach (var property in output.GetType().GetProperties())
            {
                var parameterInfo = property.GetCustomAttribute(typeof(FunctionParameterAttribute), true) as FunctionParameterAttribute;
                if (parameterInfo == null) parameterInfo = new FunctionParameterAttribute();
                if (parameterInfo.IsRequired)
                {
                    requiredProperties.Add(property);
                }

                switch(parameterInfo.Source)
                {
                    case ParameterIn.Query:
                        queryProperties.Add(property);
                        break;
                    case ParameterIn.Header:
                        headerProperties.Add(property);
                        break;
                    default: throw new ApplicationException("Can't get parameters from " + parameterInfo.Source);
                }
            }

            // Common way to read a property. Returns true if there was a property name match
            bool DigestProperty(PropertyInfo property, string rawParameterName, string value, string prefix = null)
            {
                var propertyName = property.Name.ToLower();
                var parameterInfo = property.GetCustomAttribute(typeof(FunctionParameterAttribute), true) as FunctionParameterAttribute;
                if(parameterInfo?.FixPropertyName != null)
                {
                    var parts = parameterInfo.FixPropertyName.Split(new char[] { ',' }, 2);
                    if (parts.Length != 2)
                    {
                        throw new ArgumentException($"Bad 'FixPropertyName' value on FunctionParameter '{propertyName}': {parameterInfo.FixPropertyName}");
                    }
                    propertyName = property.Name.Replace(parts[0], parts[1]).ToLower();
                }
                var parameterName = rawParameterName.ToLower();
                if (propertyName == parameterName)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            if (!value.StartsWith(prefix))
                            {
                                errors.Add($"Error on ({property.PropertyType.Name}) property '{property.Name}': Required prefix '{prefix}' was missing.");
                                return true;
                            }
                            value = value.Substring(prefix.Length);
                        }

                        if (property.PropertyType.IsArray)
                        {
                            var parts = value.Split(',');
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
                            property.SetValue(output, ParseValue(property.PropertyType, value));
                        }
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Error on ({property.PropertyType.Name}) property '{property.Name}': {e.Message}");
                    }
                    if (requiredProperties.Contains(property)) requiredProperties.Remove(property);
                    return true;
                }

                return false;
            }

            foreach (var uriParameter in GetQueryNameValuePairs(request))
            {
                var foundIt = false;
                foreach (var property in queryProperties)
                {
                    if (DigestProperty(property, uriParameter.Key, uriParameter.Value))
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt) errors.Add($"Unknown uri parameter '{uriParameter.Key}'");
            }

            foreach (var headerItem in request.Headers)
            {
                var headerValues = headerItem.Value.ToArray();

                foreach (var property in headerProperties)
                {
                    var parameterInfo = property.GetCustomAttribute(typeof(FunctionParameterAttribute), true) as FunctionParameterAttribute;

                    if (DigestProperty(property, headerItem.Key, headerValues[0], parameterInfo?.RemoveRequiredPrefix))
                    {
                        break;
                    }
                }
            }

            foreach (var property in requiredProperties)
            {
                errors.Add($"Missing required parameter '{property.Name}'");
            }

            if (errors.Count > 0) throw new ServiceOperationException(ServiceOperationError.BadParameter, string.Join("\r\n", errors));
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

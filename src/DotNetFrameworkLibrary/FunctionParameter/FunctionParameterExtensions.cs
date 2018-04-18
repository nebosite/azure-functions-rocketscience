using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// This is the base class for a property bag that describes parameters to a 
    /// function. 
    /// </summary>
    //--------------------------------------------------------------------------------
    public static class FunctionParameterExtensions
    {
        //------------------------------------------------------------------------------
        /// <summary>
        /// Yes, I know this is provided in system.web.http, but the point of this library
        /// is to avoid that dependency
        /// </summary>
        //------------------------------------------------------------------------------
        public static KeyValuePair<string,string>[] GetQueryNameValuePairs(this HttpRequestMessage request) 
        {
            var queryParts = request.RequestUri.Query.TrimStart('?').Split('&');
            var output = new List<KeyValuePair<string, string>>();
            foreach(var part in queryParts)
            {
                var trimmed = part.Trim();
                if (trimmed == "") continue;
                var subParts = trimmed.Split(new[] { '=' }, 2);
                output.Add(new KeyValuePair<string, string>(subParts[0], subParts.Length > 1 ?  Uri.UnescapeDataString(subParts[1]).Trim() : null));
            }

            return output.ToArray();
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Generic way to handle parameters
        /// </summary>
            //------------------------------------------------------------------------------
        public static T ReadParameters<T>(this HttpRequestMessage request) where T : new()
        {
            T output = new T();
            var uriProperties = new List<PropertyInfo>();
            var headerProperties = new List<PropertyInfo>();
            var errors = new List<string>();

            var requiredProperties = new List<PropertyInfo>();
            foreach (var property in output.GetType().GetProperties())
            {
                uriProperties.Add(property);
                var apiAttribute = property.GetCustomAttribute(typeof(FunctionParameterRequiredAttribute), true);
                if (apiAttribute != null)
                {
                    requiredProperties.Add(property);
                }

                var fromHeaderAttribute = property.GetCustomAttribute(typeof(FunctionParameterFromHeaderAttribute), true);
                if (fromHeaderAttribute != null)
                {
                    uriProperties.Remove(property);
                    headerProperties.Add(property);
                }               
            }

            // Common way to read a property. Returns true if there was a property name match
            bool DigestProperty(PropertyInfo property, string key, string value, string prefix = null)
            {
                var propertyName = property.Name.ToLower();
                var dollarName = property.Name.ToLower().Replace("__", "$");
                var parameterName = key.ToLower();
                if (propertyName == parameterName || dollarName == parameterName)
                {
                    try
                    {
                        if(!string.IsNullOrEmpty(prefix))
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

            foreach (var uriParameter in request.GetQueryNameValuePairs())
            {
                var foundIt = false;
                foreach (var property in uriProperties)
                {
                    if(DigestProperty(property, uriParameter.Key, uriParameter.Value))
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
                    var fromHeaderAttribute = property.GetCustomAttribute(typeof(FunctionParameterFromHeaderAttribute), true) as FunctionParameterFromHeaderAttribute;

                    if (DigestProperty(property, headerItem.Key, headerValues[0], fromHeaderAttribute.RemoveRequiredPrefix))
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
                case "Int32": return Int32.Parse(value);
                case "Double": return Double.Parse(value);
                case "DateTime": return DateTime.Parse(value);
                default: throw new ApplicationException($"Unknown type: {type.Name}");
            }
        }
    }
}

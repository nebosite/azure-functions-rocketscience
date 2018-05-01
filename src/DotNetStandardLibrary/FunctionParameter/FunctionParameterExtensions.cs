using Swagger.ObjectModel;
using System;
using System.Reflection;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //------------------------------------------------------------------------------
    /// <summary>
    /// Special extensions to help manage function parameters
    /// </summary>
    //------------------------------------------------------------------------------
    public static class FunctionParameterExtensions
    {
        public static FunctionParameterAttribute GetParams(this PropertyInfo property)
        {
            var parameterInfo = property.GetCustomAttribute(typeof(FunctionParameterAttribute), true) as FunctionParameterAttribute;
            return parameterInfo == null ? new FunctionParameterAttribute() : parameterInfo;
        }

        public static string GetSourcePropertyName(this PropertyInfo property)
        { 
            var parameterInfo = property.GetParams();
            return parameterInfo.SourcePropertyName != null ? parameterInfo.SourcePropertyName : property.Name;
        }
    }
}

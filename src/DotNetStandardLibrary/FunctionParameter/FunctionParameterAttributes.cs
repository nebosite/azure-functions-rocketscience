using System;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// For properties that are required
    /// </summary>
    //--------------------------------------------------------------------------------
    public class FunctionParameterRequiredAttribute : Attribute { }

    //--------------------------------------------------------------------------------
    /// <summary>
    /// Signifies that this parameter is a header value
    /// </summary> 
    //--------------------------------------------------------------------------------
    public class FunctionParameterFromHeaderAttribute : Attribute
    {
        /// <summary>
        /// The header value must have the specified prefix, which will be removed
        /// </summary>
        public string RemoveRequiredPrefix { get; set; }
    }
}

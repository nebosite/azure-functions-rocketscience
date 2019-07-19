using Swagger.ObjectModel;
using System;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //------------------------------------------------------------------------------
    /// <summary>
    /// Modifiers to control how HttpRequest data makes it into your function
    /// </summary>
    //------------------------------------------------------------------------------
    public class FunctionParameterAttribute : Attribute
    {
        /// <summary>
        /// Indicates where the data for this parameter should come from
        /// </summary>
        public ParameterIn Source { get; set; } = ParameterIn.Query;

        /// <summary>
        /// If true, will return a friendly error when parameter is missing
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Set this to true if the property should not be considered as a parameter.
        /// If true, the RocketScience won't try to fill this value, but if it is
        /// specified in the call, there will be an unknown parameter error
        /// </summary>
        public bool Ignore { get; set; } = false;

        /// <summary>
        /// If specified, will give an error if the prefix is not present
        /// otherwise will set the parameter value after removing prefix.
        /// e.g.:  Remove "Bearer " from an authorization token
        /// </summary>
        public string RemoveRequiredPrefix { get; set; }

        /// <summary>
        /// Get the data for theis property using this name instead of
        /// the actual property name.
        /// E.g. $top   
        /// </summary>
        public string SourcePropertyName { get; set; }

        #region SWAGGER 

        /// <summary>
        /// Text to display in the public docs for this API
        /// </summary>
        public string SwaggerDescription { get; set; }

        /// <summary>
        /// This is a signal to swagger to hide the contents
        /// of this parameter when specified from a UI
        /// </summary>
        public bool SwaggerSecurityParameter { get; set; } = false;

        #endregion // SWAGGER
    }
}

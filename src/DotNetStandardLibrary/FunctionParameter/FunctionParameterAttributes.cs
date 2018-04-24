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
        /// If specified, will give an error if the prefix is not present
        /// otherwise will set the parameter value after removing prefix.
        /// e.g.:  Remove "Bearer " from an authorization token
        /// </summary>
        public string RemoveRequiredPrefix { get; set; }

        /// <summary>
        /// Allows for the use of special characters in the query.
        /// Format:  (ReplaceTextInPropertyName),(WithThis)
        /// E.g. $top   
        /// </summary>
        public string FixPropertyName { get; set; }

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

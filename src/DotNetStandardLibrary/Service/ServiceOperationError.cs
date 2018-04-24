
namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Error codes for the productivity service
    /// </summary>
    //--------------------------------------------------------------------------------
    public enum ServiceOperationError
    {
        NoError,
        FatalError,
        AuthorizationError,
        BadParameter,
        DataError,
    }
}

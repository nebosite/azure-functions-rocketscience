
namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Error codes for the productivity service
    /// </summary>
    //--------------------------------------------------------------------------------
    public enum ServiceOperationError
    {
        NoError = 0,
        FatalError = 1,
        AuthorizationError = 2,
        BadParameter = 2,
        DataError = 3,
    }
}

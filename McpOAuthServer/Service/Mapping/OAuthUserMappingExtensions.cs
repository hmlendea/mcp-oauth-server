using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Mapping
{
    internal static class OAuthUserMappingExtensions
    {
        internal static OAuthUser ToServiceModel(this OAuthUserDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            Username = dataObject.Username,
            PasswordHash = dataObject.PasswordHash,
            Email = dataObject.Email,
            DisplayName = dataObject.DisplayName,
            IsEmailVerified = dataObject.IsEmailVerified
        };

        internal static OAuthUserDataObject ToDataObject(this OAuthUser domainModel) => new()
        {
            Id = domainModel.Id,
            Username = domainModel.Username,
            PasswordHash = domainModel.PasswordHash,
            Email = domainModel.Email,
            DisplayName = domainModel.DisplayName,
            IsEmailVerified = domainModel.IsEmailVerified
        };
    }
}

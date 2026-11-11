using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Mapping
{
    internal static class OAuthClientMappingExtensions
    {
        internal static OAuthClient ToServiceModel(this OAuthClientDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            Name = dataObject.Name,
            RedirectUris = dataObject.RedirectUris,
            AllowedScopes = dataObject.AllowedScopes,
            IsPublic = dataObject.IsPublic,
            SecretHash = dataObject.SecretHash,
            CreatedDateTime = TimestampConverter.Parse(dataObject.CreatedTimestamp)
        };

        internal static OAuthClientDataObject ToDataObject(this OAuthClient domainModel) => new()
        {
            Id = domainModel.Id,
            Name = domainModel.Name,
            RedirectUris = domainModel.RedirectUris,
            AllowedScopes = domainModel.AllowedScopes,
            IsPublic = domainModel.IsPublic,
            SecretHash = domainModel.SecretHash,
            CreatedTimestamp = TimestampConverter.Format(domainModel.CreatedDateTime)
        };
    }
}

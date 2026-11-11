using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Mapping
{
    internal static class RefreshTokenMappingExtensions
    {
        internal static RefreshToken ToServiceModel(this RefreshTokenDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            ClientId = dataObject.ClientId,
            UserId = dataObject.UserId,
            Scope = dataObject.Scope,
            Audience = dataObject.Audience,
            Nonce = dataObject.Nonce,
            ExpiryDateTime = TimestampConverter.Parse(dataObject.ExpiryTimestamp)
        };

        internal static RefreshTokenDataObject ToDataObject(this RefreshToken domainModel) => new()
        {
            Id = domainModel.Id,
            ClientId = domainModel.ClientId,
            UserId = domainModel.UserId,
            Scope = domainModel.Scope,
            Audience = domainModel.Audience,
            Nonce = domainModel.Nonce,
            ExpiryTimestamp = TimestampConverter.Format(domainModel.ExpiryDateTime)
        };
    }
}

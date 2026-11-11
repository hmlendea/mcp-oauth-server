using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Mapping
{
    internal static class AuthorisationCodeMappingExtensions
    {
        internal static AuthorisationCode ToServiceModel(this AuthorisationCodeDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            ClientId = dataObject.ClientId,
            UserId = dataObject.UserId,
            RedirectUri = dataObject.RedirectUri,
            Scope = dataObject.Scope,
            Audience = dataObject.Audience,
            CodeChallenge = dataObject.CodeChallenge,
            Nonce = dataObject.Nonce,
            AuthenticationDateTime = TimestampConverter.Parse(dataObject.AuthenticationTimestamp),
            ExpiryDateTime = TimestampConverter.Parse(dataObject.ExpiryTimestamp)
        };

        internal static AuthorisationCodeDataObject ToDataObject(this AuthorisationCode domainModel) => new()
        {
            Id = domainModel.Id,
            ClientId = domainModel.ClientId,
            UserId = domainModel.UserId,
            RedirectUri = domainModel.RedirectUri,
            Scope = domainModel.Scope,
            Audience = domainModel.Audience,
            CodeChallenge = domainModel.CodeChallenge,
            Nonce = domainModel.Nonce,
            AuthenticationTimestamp = TimestampConverter.Format(domainModel.AuthenticationDateTime),
            ExpiryTimestamp = TimestampConverter.Format(domainModel.ExpiryDateTime)
        };
    }
}

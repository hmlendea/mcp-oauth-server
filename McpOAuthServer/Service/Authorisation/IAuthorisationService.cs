using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Authorisation
{
    public interface IAuthorisationService
    {
        AuthorisationValidation Validate(AuthorisationGrant request);

        string CreateCode(
            AuthorisationGrant request,
            AuthorisationValidation validation,
            string userId);
    }
}
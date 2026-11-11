using System.Collections.Generic;

using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.AuthorisationCodes
{
    public interface IAuthorisationCodeService
    {
        string Create(AuthorisationCode authorisationCode);

        AuthorisationCode Consume(string code);
    }
}
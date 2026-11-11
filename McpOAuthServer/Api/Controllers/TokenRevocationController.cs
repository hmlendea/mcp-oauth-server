using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Requests;
using McpOAuthServer.Api.Responses;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.RefreshTokens;

namespace McpOAuthServer.Api.Controllers
{
    [Route("revoke")]
    [ApiController]
    public sealed class TokenRevocationController(
        IClientService clientService,
        IRefreshTokenService refreshTokenService) : ControllerBase
    {
        [HttpPost]
        public ActionResult Revoke([FromForm] RevokeTokenRequest request)
        {
            ClientAuthentication authentication = clientService.Authenticate(
                request.ClientId,
                request.ClientSecret);

            if (!authentication.IsAuthenticated)
            {
                return new JsonResult(new GetErrorResponse
                {
                    Error = "invalid_client",
                    ErrorDescription = "The client credentials are invalid."
                })
                {
                    StatusCode = 401
                };
            }

            refreshTokenService.Revoke(request.Token);

            return new OkResult();
        }
    }
}
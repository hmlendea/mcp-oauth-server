using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Requests;
using McpOAuthServer.Api.Responses;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Tokens;

namespace McpOAuthServer.Api.Controllers
{
    [Route("introspect")]
    [ApiController]
    public sealed class TokenIntrospectionController(
        ITokenService tokenService) : ControllerBase
    {
        [HttpPost]
        public ActionResult Introspect([FromForm] RevokeTokenRequest request)
        {
            TokenIntrospection introspection = tokenService.Introspect(request.Token);
            long issuedAt = 0;
            long expiresAt = 0;

            if (introspection.IsActive)
            {
                issuedAt = introspection.IssuedAtDateTime.ToUnixTimeSeconds();
                expiresAt = introspection.ExpiryDateTime.ToUnixTimeSeconds();
            }

            return new JsonResult(new GetTokenIntrospectionResponse
            {
                IsActive = introspection.IsActive,
                Scope = introspection.Scope,
                ClientId = introspection.ClientId,
                Subject = introspection.UserId,
                Audience = introspection.Audience,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt
            })
            {
                StatusCode = 200
            };
        }
    }
}
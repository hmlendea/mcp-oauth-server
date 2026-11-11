using System;

using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Mapping;
using McpOAuthServer.Api.Requests;
using McpOAuthServer.Api.Responses;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Tokens;

namespace McpOAuthServer.Api.Controllers
{
    [Route("token")]
    [ApiController]
    public sealed class TokenController(
        ITokenExchangeService tokenExchangeService) : ControllerBase
    {
        [HttpPost]
        public ActionResult Exchange([FromForm] ExchangeTokenRequest request)
        {
            TokenExchangeResult result = tokenExchangeService.Exchange(request.ToServiceModel());

            if (!result.IsSuccessful)
            {
                int statusCode = 400;

                if (string.Equals(result.Error, "invalid_client", StringComparison.Ordinal))
                {
                    statusCode = 401;
                }

                return new JsonResult(new GetErrorResponse
                {
                    Error = result.Error,
                    ErrorDescription = result.ErrorDescription
                })
                {
                    StatusCode = statusCode
                };
            }

            IssuedTokens tokens = result.Tokens;

            return new JsonResult(new GetTokenResponse
            {
                AccessToken = tokens.AccessToken,
                TokenType = tokens.TokenType,
                ExpiresInSeconds = tokens.ExpiresInSeconds,
                Scope = tokens.Scope,
                IdToken = tokens.IdToken,
                RefreshToken = tokens.RefreshToken
            })
            {
                StatusCode = 200
            };
        }
    }
}
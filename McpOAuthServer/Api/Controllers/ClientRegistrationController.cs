using System;

using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Mapping;
using McpOAuthServer.Api.Requests;
using McpOAuthServer.Api.Responses;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Api.Controllers
{
    [Route("register")]
    [ApiController]
    public sealed class ClientRegistrationController(
        IClientService clientService) : ControllerBase
    {
        private static string PublicAuthenticationMethod => "none";

        private static string ConfidentialAuthenticationMethod => "client_secret_post";

        [HttpPost]
        public ActionResult Register([FromBody] RegisterClientRequest request)
        {
            ClientRegistrationResult result = clientService.Register(request.ToServiceModel());

            if (!result.IsSuccessful)
            {
                return new JsonResult(new GetErrorResponse
                {
                    Error = "invalid_redirect_uri",
                    ErrorDescription = result.ErrorDescription
                })
                {
                    StatusCode = 400
                };
            }

            RegisteredClient client = result.Client;
            string authenticationMethod = ConfidentialAuthenticationMethod;

            if (client.IsPublic)
            {
                authenticationMethod = PublicAuthenticationMethod;
            }

            return new JsonResult(new GetRegisteredClientResponse
            {
                ClientId = client.ClientId,
                ClientSecret = client.ClientSecret,
                ClientName = client.Name,
                ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Scope = client.Scope,
                TokenEndpointAuthenticationMethod = authenticationMethod
            })
            {
                StatusCode = 201
            };
        }
    }
}
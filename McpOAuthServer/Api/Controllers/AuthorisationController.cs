using System;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Mapping;
using McpOAuthServer.Api.Models;
using McpOAuthServer.Api.Requests;
using McpOAuthServer.Configuration;
using McpOAuthServer.Service.Authorisation;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Users;

namespace McpOAuthServer.Api.Controllers
{
    [Route("authorize")]
    [ApiController]
    public sealed class AuthorisationController(
        IAuthorisationService authorisationService,
        IClientService clientService,
        IUserService userService,
        OAuthServerSettings settings) : ControllerBase
    {
        private static string DeniedDecision => "deny";

        [HttpGet]
        public ActionResult Authorise([FromQuery] AuthoriseRequest request)
        {
            AuthorisationGrant serviceRequest = request.ToServiceModel();
            AuthorisationValidation validation = authorisationService.Validate(serviceRequest);

            OAuthClient client = clientService.TryGet(serviceRequest.ClientId);

            if (!validation.IsValid)
            {
                return RenderError(serviceRequest, validation);
            }

            if (!IsRedirectUriRegistered(client, validation.RedirectUri))
            {
                return BadRequest();
            }

            return RenderForm(serviceRequest, validation, client.Name, string.Empty);
        }

        [HttpPost]
        public ActionResult AuthoriseDecision([FromForm] AuthoriseDecisionRequest request)
        {
            AuthorisationGrant serviceRequest = request.ToServiceModel();
            AuthorisationValidation validation = authorisationService.Validate(serviceRequest);

            if (!validation.IsValid)
            {
                return RenderError(serviceRequest, validation);
            }

            OAuthClient client = clientService.Get(serviceRequest.ClientId);

            if (!IsRedirectUriRegistered(client, validation.RedirectUri))
            {
                return BadRequest();
            }

            if (string.Equals(request.Decision, DeniedDecision, StringComparison.Ordinal))
            {
                return Redirect(AuthorisationPageRenderer.BuildErrorRedirect(
                    AuthorisationRedirectFactory.CreateErrorRedirect(
                        validation.RedirectUri,
                        "access_denied",
                        "The authorisation request was denied.",
                        serviceRequest.State,
                        settings.Issuer)));
            }

            UserAuthentication authentication = userService.Authenticate(
                serviceRequest.Username,
                serviceRequest.Password);

            if (!authentication.IsAuthenticated)
            {
                return RenderForm(
                    serviceRequest,
                    validation,
                    client.Name,
                    "The supplied credentials are not valid.");
            }

            string code = authorisationService.CreateCode(
                serviceRequest,
                validation,
                authentication.User.Id);

            return Redirect(AuthorisationPageRenderer.BuildCodeRedirect(
                AuthorisationRedirectFactory.CreateCodeRedirect(
                    validation.RedirectUri,
                    code,
                    serviceRequest.State,
                    settings.Issuer)));
        }

        private ContentResult RenderForm(
            AuthorisationGrant request,
            AuthorisationValidation validation,
            string clientName,
            string errorMessage)
            => new()
            {
                ContentType = "text/html; charset=utf-8",
                StatusCode = 200,
                Content = AuthorisationPageRenderer.RenderForm(new()
                {
                    ClientName = clientName,
                    Scopes = validation.ScopeValues,
                    ResponseType = request.ResponseType,
                    ClientId = request.ClientId,
                    RedirectUri = request.RedirectUri,
                    Scope = validation.Scope,
                    State = request.State,
                    CodeChallenge = request.CodeChallenge,
                    CodeChallengeMethod = request.CodeChallengeMethod,
                    Resource = request.Resource,
                    Nonce = request.Nonce,
                    ErrorMessage = errorMessage
                })
            };

        private ActionResult RenderError(
            AuthorisationGrant request,
            AuthorisationValidation validation)
        {
            OAuthClient client = clientService.TryGet(request.ClientId);

            if (string.IsNullOrWhiteSpace(validation.RedirectUri) ||
                !IsRedirectUriRegistered(client, validation.RedirectUri))
            {
                return new ContentResult
                {
                    ContentType = "text/plain; charset=utf-8",
                    StatusCode = 400,
                    Content = $"{validation.Error}: {validation.ErrorDescription}"
                };
            }

            return Redirect(AuthorisationPageRenderer.BuildErrorRedirect(
                AuthorisationRedirectFactory.CreateErrorRedirect(
                    validation.RedirectUri,
                    validation.Error,
                    validation.ErrorDescription,
                    request.State,
                    settings.Issuer)));
        }

        private static bool IsRedirectUriRegistered(OAuthClient client, string redirectUri)
            => client is not null &&
                !string.IsNullOrWhiteSpace(redirectUri) &&
                client.RedirectUris.Contains(redirectUri, StringComparer.Ordinal);
    }
}
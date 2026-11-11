using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Responses;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Api.Controllers
{
    [Route("jwks")]
    [ApiController]
    public sealed class JsonWebKeySetController(
        ITokenSigningKeyProvider keyProvider) : ControllerBase
    {
        [HttpGet]
        public ActionResult Get() => new JsonResult(new GetJsonWebKeySetResponse
        {
            Keys =
            [
                new GetJsonWebKeyResponse
                {
                    KeyId = keyProvider.KeyId,
                    Modulus = keyProvider.Modulus,
                    Exponent = keyProvider.Exponent
                }
            ]
        })
        {
            StatusCode = 200
        };
    }
}

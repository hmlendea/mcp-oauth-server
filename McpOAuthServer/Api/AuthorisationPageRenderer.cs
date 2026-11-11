using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using McpOAuthServer.Api.Models;

namespace McpOAuthServer.Api
{
    public static class AuthorisationPageRenderer
    {
        private static string HeadingText => "Authorise Access";

        private static string IssuerParameterName => "iss";

        private static string StateParameterName => "state";

        public static string BuildCodeRedirect(AuthorisationRedirect redirect)
            => BuildRedirect(redirect);

        public static string BuildErrorRedirect(AuthorisationRedirect redirect)
            => BuildRedirect(redirect);

        public static string RenderForm(AuthorisationPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            StringBuilder builder = new();

            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html lang=\"en\">");
            builder.AppendLine("<head>");
            builder.AppendLine("<meta charset=\"utf-8\">");
            builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            builder.AppendLine("<title>Authorise Access</title>");
            builder.AppendLine("<style>body{font-family:system-ui,sans-serif;max-width:32rem;margin:3rem auto;padding:0 1rem}fieldset{border:1px solid #ccc;border-radius:.5rem;padding:1rem}label{display:block;margin:.75rem 0 .25rem}input[type=text],input[type=password]{width:100%;padding:.5rem;box-sizing:border-box}button{margin-top:1rem;padding:.5rem 1rem}ul{padding-left:1.25rem}.error{color:#b00020}</style>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body>");
            builder.AppendLine($"<h1>{EncodeHtml(HeadingText)}</h1>");

            if (!string.IsNullOrEmpty(page.ErrorMessage))
            {
                builder.AppendLine($"<p class=\"error\">{EncodeHtml(page.ErrorMessage)}</p>");
            }

            builder.AppendLine($"<p><strong>{EncodeHtml(page.ClientName)}</strong> is requesting access to the following permissions:</p>");
            builder.AppendLine("<ul>");

            foreach (string scope in page.Scopes)
            {
                builder.AppendLine($"<li>{EncodeHtml(scope)}</li>");
            }

            builder.AppendLine("</ul>");
            builder.AppendLine("<form method=\"post\" action=\"/authorize\">");
            builder.AppendLine(HiddenField("response_type", page.ResponseType));
            builder.AppendLine(HiddenField("client_id", page.ClientId));
            builder.AppendLine(HiddenField("redirect_uri", page.RedirectUri));
            builder.AppendLine(HiddenField("scope", page.Scope));
            builder.AppendLine(HiddenField("state", page.State));
            builder.AppendLine(HiddenField("code_challenge", page.CodeChallenge));
            builder.AppendLine(HiddenField("code_challenge_method", page.CodeChallengeMethod));
            builder.AppendLine(HiddenField("resource", page.Resource));
            builder.AppendLine(HiddenField("nonce", page.Nonce));
            builder.AppendLine("<fieldset>");
            builder.AppendLine("<legend>Sign in</legend>");
            builder.AppendLine("<label for=\"username\">Username</label>");
            builder.AppendLine("<input id=\"username\" name=\"username\" type=\"text\" autocomplete=\"username\" required>");
            builder.AppendLine("<label for=\"password\">Password</label>");
            builder.AppendLine("<input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"current-password\" required>");
            builder.AppendLine("</fieldset>");
            builder.AppendLine("<button type=\"submit\" name=\"decision\" value=\"approve\">Approve</button>");
            builder.AppendLine("<button type=\"submit\" name=\"decision\" value=\"deny\">Deny</button>");
            builder.AppendLine("</form>");
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");

            return builder.ToString();
        }

        private static string BuildRedirect(AuthorisationRedirect redirect)
        {
            ArgumentNullException.ThrowIfNull(redirect);

            List<string> pairs = [];

            foreach (KeyValuePair<string, string> parameter in redirect.Parameters)
            {
                pairs.Add($"{EncodeQuery(parameter.Key)}={EncodeQuery(parameter.Value)}");
            }

            if (!string.IsNullOrEmpty(redirect.State))
            {
                pairs.Add($"{StateParameterName}={EncodeQuery(redirect.State)}");
            }

            if (!string.IsNullOrEmpty(redirect.Issuer))
            {
                pairs.Add($"{IssuerParameterName}={EncodeQuery(redirect.Issuer)}");
            }

            string query = string.Join('&', pairs);
            string separator = "?";

            if (redirect.RedirectUri.Contains('?', StringComparison.Ordinal))
            {
                separator = "&";
            }

            return $"{redirect.RedirectUri}{separator}{query}";
        }

        private static string HiddenField(string name, string value)
            => $"<input type=\"hidden\" name=\"{EncodeHtml(name)}\" value=\"{EncodeHtml(value)}\">";

        private static string EncodeHtml(string value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            return WebUtility.HtmlEncode(value);
        }

        private static string EncodeQuery(string value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            return Uri.EscapeDataString(value);
        }
    }
}
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Host.Base
{
    public class JwtUtils
    {
        public static async Task<ClaimsPrincipal> ValidateAccessToken(string accessToken)
        {
            const string auth0Domain = "https://darchukoleksandr.eu.auth0.com/";
            const string auth0Audience = "https://darchukoleksandr.eu.auth0.com/userinfo";
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{auth0Domain}.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            TokenValidationParameters validationParameters =
                new TokenValidationParameters
                {
                    ValidIssuer = auth0Domain,
                    ValidAudiences = new[] { auth0Audience },
                    IssuerSigningKeys = openIdConfig.SigningKeys
                };

            SecurityToken validatedToken;
            var handler = new JwtSecurityTokenHandler();
            var result = handler.ValidateToken(accessToken, validationParameters, out validatedToken);
            return result;
        }

        public static IEnumerable<Claim> ReadUserClaims(string identityToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(identityToken);
            return jwtToken.Claims;
        }
    }
}

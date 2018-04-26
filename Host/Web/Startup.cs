using System;
using Auth0.Owin;
using Host.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Security.Jwt;
using Owin;
using TokenValidationParameters = System.IdentityModel.Tokens.TokenValidationParameters;

[assembly: OwinStartup(typeof(Startup))]
namespace Host.Web
{
    public class Startup
    {
        private static void Main()
        {
            using (WebApp.Start<Startup>("http://localhost:42512/")) 
            {
                Console.ReadLine();
            }
        }

        public void Configuration(IAppBuilder app)
        {

            var keyResolver = new OpenIdConnectSigningKeyResolver("https://darchukoleksandr.eu.auth0.com/");
            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudience = "https://darchukoleksandr.eu.auth0.com/userinfo",
                    ValidIssuer = "https://darchukoleksandr.eu.auth0.com/",
                    IssuerSigningKeyResolver = (token, securityToken, identifier, parameters) => keyResolver.GetSigningKey(identifier)
                }
            }); 

            app.MapSignalR(new HubConfiguration
            {
                EnableDetailedErrors = true
            });

            Console.WriteLine("signalr linked");
        }
    }
}

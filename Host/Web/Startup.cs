using System;
using System.Collections.Generic;
using Auth0.Owin;
using Host.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.StaticFiles;
using Owin;
using TokenValidationParameters = System.IdentityModel.Tokens.TokenValidationParameters;

[assembly: OwinStartup(typeof(Startup))]
namespace Host.Web
{
    public class Startup
    {
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

            var physicalFileSystem = new PhysicalFileSystem("");

            app.UseFileServer(new FileServerOptions
            {
                RequestPath = PathString.Empty,
                EnableDefaultFiles = true,
                FileSystem = physicalFileSystem,
                StaticFileOptions =
                {
                    RequestPath = new PathString("/*"),
                    FileSystem = physicalFileSystem,
                    ServeUnknownFileTypes = true
                },
                DefaultFilesOptions = { DefaultFileNames = new[] { "index.html" } }
            });

            app.MapSignalR(new HubConfiguration
            {
                EnableDetailedErrors = true
            });

            Console.WriteLine("signalr linked");
        }
    }
}

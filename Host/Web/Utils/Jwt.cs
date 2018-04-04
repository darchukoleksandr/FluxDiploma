using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Domain.Models;

namespace Host.Web.Utils
{
    public class Jwt
    {
        public static async Task<IEnumerable<TypeValueClaim>> ReadUserClaims(string accesToken)
        {
            var client = new AuthenticationApiClient(new Uri("https://darchukoleksandr.eu.auth0.com/"));
            var userInfo = await client.GetUserInfoAsync(accesToken);

            var result = new List<TypeValueClaim>
            {
                new TypeValueClaim
                {
                    Type = nameof(userInfo.Email),
                    Value = userInfo.Email
                },
                new TypeValueClaim
                {
                    Type = nameof(userInfo.NickName),
                    Value = userInfo.NickName
                },
                new TypeValueClaim
                {
                    Type = nameof(userInfo.FirstName),
                    Value = userInfo.FirstName
                },
                new TypeValueClaim
                {
                    Type = nameof(userInfo.LastName),
                    Value = userInfo.LastName
                },
                new TypeValueClaim
                {
                    Type = nameof(userInfo.Picture),
                    Value = userInfo.Picture
                },
                new TypeValueClaim
                {
                    Type = nameof(userInfo.Gender),
                    Value = userInfo.Gender
                }
            };

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// NETCore 5.0.0
namespace DoctibleApiHubExample
{
    class Program
    {
        const String API_KEY = "fea520e24a39c3cb4134"; // provided by Doctible
        const String JWT_ALGO = "ES256"; // digital signature algorithm
        const String SCOPE = "profile"; // different API endpoints require differnt scope
        const String ENDPOINT = "https://fairbill.me/api_hub"; // may change in future

        static async Task Main(string[] args)
        {
            /* How to generate a ECDSA key pair using openSSL:
             *
               openssl ecparam -name prime256v1 -genkey -noout -out private_key.pem
               openssl ec -in private_key.pem -pubout > public_key.pem

            *  only public_key.pem should be provided to Doctible
            */
            var eccPem = File.ReadAllText("../../../private_key.pem");

            // read private key from pem file
            var key = ECDsa.Create();
            key.ImportFromPem(eccPem);
            var jwt = CreateSignedJwt(key);

            HttpClient client = new HttpClient();
            // add Authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync( ENDPOINT + "/profile");

            System.Console.Write(await response.Content.ReadAsStringAsync());
        }

        private static string CreateSignedJwt(ECDsa key)
        {
            var now = DateTime.UtcNow;
            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(new SecurityTokenDescriptor
            {
                NotBefore = now,
                Expires = now.AddMinutes(30),
                IssuedAt = now,
                Claims = new Dictionary<string, object> { { "k", API_KEY }, { "s", SCOPE} },
                SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(key), JWT_ALGO)
            });
        }

    }
}

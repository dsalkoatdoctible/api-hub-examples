using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

// NETCore 5.0.0
namespace DoctibleApiHubExample
{
    class Program
    {
        const String API_KEY = "fea520e24a39c3cb4134"; // provided by Doctible
        const String JWT_ALGO = "ES256"; // digital signature algorithm
        const String SCOPE = "profile"; // different API endpoints require differnt scope
        const String ENDPOINT = "https://www.fairbill.me/api_hub/v1"; // may change in future

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
            System.Console.WriteLine("Consumer Token:");
            System.Console.WriteLine(jwt);
            System.Console.WriteLine("");

            /*** /locations ***/

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync( ENDPOINT + "/locations");
            System.Console.WriteLine("Locations:\n---------------");
            var locationsData = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var locations = locationsData.RootElement.GetProperty("locations");
            String lastLocationToken = "";

            foreach(var location in locations.EnumerateArray())
            {
                System.Console.WriteLine("ID: " + location.GetProperty("id").GetString());
                System.Console.WriteLine("Name: " + location.GetProperty("name").GetString());
                System.Console.WriteLine("Location token: " + location.GetProperty("location_token").GetString());
                System.Console.WriteLine("---------------");
                lastLocationToken = location.GetProperty("location_token").GetString();
            }

            /*** /treatments ***/

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lastLocationToken); // use location token
            response = await client.GetAsync(ENDPOINT + "/treatments");
            System.Console.WriteLine("\n\nTreatments:\n---------------");
            var treatmentsData = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var treatments = treatmentsData.RootElement.GetProperty("treatments");

            foreach(var treatment in treatments.EnumerateArray())
            {
                System.Console.WriteLine(treatment.GetProperty("id").GetRawText().PadRight(10,' ') + " " + treatment.GetProperty("name").GetString());
            }

            /*** /practitioners ***/

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lastLocationToken); // use location token
            response = await client.GetAsync(ENDPOINT + "/practitioners");
            System.Console.WriteLine("\n\nPractitioners:\n---------------");
            var practitionersData = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var practitioners = practitionersData.RootElement.GetProperty("practitioners");

            foreach (var practitioner in practitioners.EnumerateArray())
            {
                System.Console.WriteLine(practitioner.GetProperty("id").GetRawText().PadRight(10, ' ') + " " + practitioner.GetProperty("first_name").GetString() + " " + practitioner.GetProperty("last_name").GetString());
            }
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

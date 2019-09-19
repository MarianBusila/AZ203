using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace KeyVaultQuickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                 .AddJsonFile("settings.json")
                 .AddJsonFile("settings.local.json")
                 .Build();

            string clientId = configRoot.GetSection("ClientId").Value;
            string clientSecret = configRoot.GetSection("ClientSecret").Value;

            KeyVaultClient kvClient = new KeyVaultClient(async (authority, resource, scope) =>
            {
                var adCredential = new ClientCredential(clientId, clientSecret);
                var authenticationContext = new AuthenticationContext(authority, null);
                return (await authenticationContext.AcquireTokenAsync(resource, adCredential)).AccessToken;
            });

            string kvURL = "https://myvault201909.vault.azure.net";
            string secretName = "SecretFromCode";
            string secretValue = "SecretValueFromCode";
            await kvClient.SetSecretAsync($"{kvURL}", secretName, secretValue);

            var keyvaultSecret = await kvClient.GetSecretAsync($"{kvURL}", secretName).ConfigureAwait(false);
            Console.WriteLine($"The secret: {keyvaultSecret.Value}");
        }
    }
}

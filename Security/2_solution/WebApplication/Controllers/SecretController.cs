using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace WebApplication.Controllers
{
    public class SecretController : Controller
    {
        private readonly IConfiguration config;

        public SecretController(IConfiguration config)
        {
            this.config = config;
        }

        public async Task<ActionResult> Secret()
        {
            //
            // When a call is made to the KeyVaultClient to retrieve a secret, key, or certificate (see GetSecretAsync below)
            // the SDK will execute a callback delegate to obtain an identity token for the identity with which it will
            // access the key vault.
            //
            // When an instance of AzureServiceTokenProvider is created with no constructor parameters the managed identity of
            // the host web app is used.
            //
            // Here, we setup a callback to obtain a token for the Managed Identity of the application,
            // i.e. AzureServiceTokenProvider.KeyVaultTokenCallback
            //
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            KeyVaultClient.AuthenticationCallback callback = new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback);

            KeyVaultClient keyVaultClient = new KeyVaultClient(callback);

            // Get key vault details from the application's configuration
            string keyVaultEndPoint = this.config["KeyVaultEndPoint"];  // Must be the base URL of the keyvault; i.e.  https://{key-vault-name}.vault.azure.net/
            string keyVaultSecretName = this.config["KeyVaultSecretName"];

            // Using the KeyVaultClient, fetch the secret from the vault.
            // This will first obtain an identity token using the callback above, and using that token will then retrieve the secret.
            SecretBundle secretBundle = await keyVaultClient.GetSecretAsync(keyVaultEndPoint, keyVaultSecretName);

            ViewBag.Message = secretBundle.Value;

            return View();
        }

        public ActionResult Secret2()
        {
            //
            // The Azure web app will automatically retrieve the secret referenced from the KeyVault by using its managed identity to
            // obtain an access token from Azure AD, and using this access token to retrieve the KeyVault secret.
            //
            ViewBag.Message = this.config["KeyVaultReferenceToSecret"];

            return View();
        }

        public ActionResult Secret3()
        {
            //
            // The .NET Core runtime will use the the configuration added to the pipeline in Program.cs. This allows .NET Core to 
            // treat the KeyVault as another source of configuration.
            //
            ViewBag.Message = this.config["MySecret"];

            return View();
        }

        public async Task<ActionResult> SqlConnection()
        {
            //
            // When an instance of AzureServiceTokenProvider is created with no constructor parameters the managed identity of
            // the host web app is used.
            //
            // An access token is retrieved to allow the application to access Azure SQL databases. The resource URI (https://database.windows.net/)
            // defines for which type of resources the application is asking for an access token.
            //
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/");

            using (var connection = new SqlConnection())
            {
                string sqlConnectionString = this.config["SqlConnectionString"];

                // The connection string does not contain the user ID or password which will be used to connect to the database.
                connection.ConnectionString = sqlConnectionString;

                // Before openning the connection, add the access token to the connection. This access token will contain the
                // ID of the managed identity, the identity provider which has signed the token (Azure AD), and a hash signature
                // of the token.  Since the database trusts the identity provider, AND it can validate that the identity provider
                // did infact sign this token, AND that the managed identity has been granted access to the database the connection
                // will be successfully completed.
                connection.AccessToken = accessToken;

                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM SalesLT.Customer";
                    command.CommandType = CommandType.Text;

                    object objResult = await command.ExecuteScalarAsync();

                    int count = Convert.ToInt32(objResult);
                    ViewBag.Message = $"There are {count} customers in the database";
                }
            }

            return View();
        }
    }
}

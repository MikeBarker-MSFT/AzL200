
# Azure PaaS Security Lab

## Clone or download the lab content
+ From clone or download the git repo

## Deploy a web app and SQL database
+ Open a powershell window
+ Execute the **Create-SecurityLab** PowerShell script found in the **0_setup** folder, with your lab ID: `.\Create-SecurityLab.ps1 -participantId {your_id}`
  + This will create a new resource group, `az{your_id}-security-rg`
  + The resource group will contain a blank web app, and a database with the **AdventureWorksLT** sample.

# Lab 1.a - Key Vault and Managed Identity
In this lab we will (a) create a managed identity for a .NET Core Web Application; (b) create a KeyVault and add a secret to it; ( c) grant access to the KeyVault for the web app's managed identity; and (d) explore ways to retrieve the secret within .NET.

### Create a Managed Identity
+ In the Azure portal (https://portal.azure.com) navigate to the web app: `az{your_id}-security-webapp`.
+ Under the **Settings** -> **Identity** blade, enable the System Assigned Managed Identity, and Save.
+ This will create a principle in Azure AD with the same name as your web app.

### Create a Key Vault
+ In the Azure portal, click on "**+ Create a resource**" and search for "Key Vault", to begin creating an Azure Key Vault.
+ Supply the following details for the KeyVault:
  + *Resource Group*: `az{your_id}-security-rg`
  + *Key vault name*: `az{your_id}-security-keyvault`
  + *Region*: `West Europe`
+ Leave all other defaults and go to the **Review + create** tab, and **Create** the key vault.

##### Add a secret to the vault
+ When the Key Vault has been created go to the key vault's blade
+ Under **Settings** -> **Secrets**, click "**+ Generate/Import**":
+ Give the secret a name and remember this for future reference. e.g. `MySecret`
+ Enter any string in the **Value** field.
+ Leaving other fields as default click **Create**.
+ The **Secrets** blade will now display the secret record in the vault.

##### Grant read access for secrets to the web app's managed identity
+ In the **Settings** -> **Access policies** blade, you should already see your identity with multiple permissions granted.
+ Click **+ Add Access Policy**
  + From the **Secret permissions** drop down select _Get_ and _List_.
  + Under **Select principal** search for your web apps identity `az{your_id}-security-webapp`. Select this principal and click **Add**, and **Add** again to give your web app access.
+ **NB**: Do not forget to click **Save** back in the "Access Policies" blade to commit the new policies.

### Add config properties to the web app
+ In the web apps blade, under **Settings** -> **Configuration**:
+ Add Application Setting for:
  + _Name_= `KeyVaultEndPoint`
  + _Value_= `https://az{your_id}-security-keyvault.vault.azure.net/`
+ Add a second application setting:
  + _Name_= `KeyVaultSecretName`
  + _Value_= `MySecret` (replace with the name of your secret)
+ **NB:** Don't forget to click "**Save**"

These settings will be used by you code to retrieve the secret from the vault.

### Retrieve the secret in code 
+ Open the `Lab.Security.sln` file in Visual Studio. The solution contains a single C# project for a .NET Core MVC web application.
+ Build the project to ensure it builds corrects.
+ Add the following NuGet packages to the project:
  + `Microsoft.Azure.KeyVault`
  + `Microsoft.Azure.Services.AppAuthentication`
+ Add a new MVC controller. Under the **Controllers** folder, right-click **Add** > **Controller...** > **MVC Controller - Empty**. Name your controller `SecurityController`.
+ Paste the following code as the class definition:
```cs
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace WebApplication.Controllers
{
    public class SecurityController : Controller
    {
        private readonly IConfiguration config;

        public SecurityController(IConfiguration config)
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
    }
}
```
 Take a moment to read the code and understand what each line is doing to obtain an access token, and using this to retreive the key vault secret.
+ Add a new MVC view. Under the **Views** folder, create a sub folder named `Security`
+ Under the **Security** folder, right-click **Add** > **View...** and name your view `Secret`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>Secret</h2>
<h3>@ViewBag.Message</h3>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, find the line:
`<li><a asp-area="" asp-controller="Home" asp-action="Index">Home</a></li>`
Under this, add a new line for the new view:
`<li><a asp-area="" asp-controller="Security" asp-action="Secret">Secret</a></li>`

### Deploy your web app to Azure
+ Right-click the project, **Publish...**
+ In the pop-up, select **Select Existing** and **Create Profile**.
  + Find your web application `az{your_id}-security-webapp`, and select it.
  + Click **Ok**, and then **Publish** to begin publishing the app.
+ When the application has been published to Azure it will open in your browser.
+ Navigate to **Secret** page from the navigation bar.

This will display your secret, having retrieved it from the Key Vault.

### Running locally (for further enrichment)
You can execute the code locally and still access the Key Vault. Add the following configuration to the **appsettings.Development.json** file:
```json
"KeyVaultEndPoint": "https://az{your_id}-security-keyvault.vault.azure.net/",
"KeyVaultSecretName": "MySecret"
```
Notice these are the same configuration details we previously added to the web application in the portal.

How is this possible? You do not have a managed identity on your local machine?!
**Answer**: The .NET Key Vault SDK will use the default Azure credentials from the Azure CLI (or Powershell). When you deployed the ARM template you would have had to login (or previously been logged-in), and this login information is stored in a local context. When the code above executes on your local machine it is using YOUR identity to retreive an access token, and access the vault. Your identity already has access to the vault because when you created it you were given access by default.
Try removing your identity principal from the Key Vault's access policies to validate this. The code will no longer be able to retreive the secret when run locally.</p>

# Lab 1.b - Using the .NET Core configuration pipeline
The application above uses a lot of code to obtain a token for the managed identity, and use this token to retrieve the secret from the vault. When a .NET Core applicaiton is started it establishes a pipeline for configuration. This pipline can be injected into to provide more sources of config than simply the applicaiton settings.  Configuration can come from files, databases, or in our case Azure Key Vault.

+ Add the NuGet package:
  + `Microsoft.Extensions.Configuration.AzureKeyVault` (**note**: add version **2.1.1** to avoid versioning conflicts)
+ The pipeline is constructed in **Program.cs**. Open this file and add the follwing using statements:
```cs
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
```
+ Replace the lines...
```cs
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>();
```
..with..
```cs
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            if (context.HostingEnvironment.IsProduction())
            {
                // Get the KeyVaultEndPoint configuration setting from the existing pipeline
                var builtConfig = config.Build();
                string keyVaultEndPoint = builtConfig["KeyVaultEndPoint"];

                // Create a KeyVaultClient with token callback
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        azureServiceTokenProvider.KeyVaultTokenCallback));

                // Add Key Vault into the configuration pipeline
                config.AddAzureKeyVault(
                    keyVaultEndPoint,
                    keyVaultClient,
                    new DefaultKeyVaultSecretManager());
            }
        })
        .UseStartup<Startup>();
```
... (i.e. Insert the call to `ConfigureAppConfiguration`, setting up Key Vault as a configuration source.)

Much of this code will be familiar from the first excercise.  The order in which the pipeline is constructed is important as this will be the order in which locations are searched to retrieve a configuration setting.  Notice here that the application settings are added first in the `CreateDefaultBuilder` method, then the Key Vault is added in `ConfigureAppConfiguration`.

+ In the **SecurityContoller** add the following method:
```cs
public ActionResult Secret2()
{
    //
    // The .NET Core runtime will use the the configuration added to the pipeline in Program.cs. This allows .NET Core to 
    // treat the KeyVault as another source of configuration.
    //
    ViewBag.Message = this.config["MySecret"];

    return View();
}
```
Unlike the previous example where the app serivce platform was responsible for retrieving the configuration, now the config pipeline we defined previously will do so.

+ Under the **Security** folder, right-click **Add** > **View...** and name your view `Secret2`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>Secret By Pipeline</h2>
<h3>@ViewBag.Message</h3>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, add a new line for the view:
`<li><a asp-area="" asp-controller="Security" asp-action="Secret2">Secret2</a></li>`

As before, publish the application and navigate to the new view to see the secret retrieved.

### A note on this method for retrieving a secret
Both the first and second options explored may be used to retrieve a secret from Key Vault, but it is worth considering which is best in a full DevOps life-cycle.

In the first method the code is implicitly aware of (a) the identity under which it is running, and (b) the fact that secrets are being stored in a Key Vault. In the second method the code is simply retrieving a configuration setting, and is not aware of the location of the setting nor how it is retrieved. (Ignoring for now the initial configuration pipeline setup).

This has the huge advantage during the software development phase that secrets can be stored in the developers local application.Development.config (do not check this file into source control), or, even better, in the user secret store; whilst in production a Key Vault can be used.  This keeps config and secrets out the code base whilst at the same time ensuring secrets are available securely.
Notice the check for `context.HostingEnvironment.IsProduction()` in the pipeline setup. Using this we can enforce that during development the application is not aware of a Key Vault.

Unfortunately, this option is only available in .NET Core ASP.NET.

# Lab 1.c - Using Key Vault references
The "Key Vault References" feature for Web Apps and Functions allows you to provide a link to the secret in the application configuration settings, and the platform will utilise its managed identity to retrieve the secret when requested.

In the following lab we will explore using this feature as a much simpler method of retrieving secrets from the Key Vault. Compare this method to the method above.

### Add Key Vault setting to the web app config
+ In the Azure portal go to you Key Vault's blade.
+ Under **Settings** -> **Secrets** select you secret (e.g. MySecret). This will show a list of versions of the secret (versioned by GUID).
+ Select the current version of the secret, copy the **Secret Identifier**. (similar to: `https://az{your_id}-security-keyvault.vault.azure.net/secrets/MySecret/abcde1234567890fabcdefabcdefabcd`)
+ In the web app's blade, under **Settings** -> **Configuration**, add Application Setting for:
  + _Name_= `SecretByReference`
  + _Value_= `@Microsoft.KeyVault(SecretUri={secret_uri})`
+ replacing `{secret_uri}` with the secret identifer copied previously.

**NB:** Remember to click **Save** to save the web app's configuration.

### Retrieve the secret in code
+ In Visual Studio, return to the **SecurityContoller** class we added previously, and add the following method:
```cs
public ActionResult Secret3()
{
    //
    // The Azure web app will automatically retrieve the secret referenced from the KeyVault by using its managed identity to
    // obtain an access token from Azure AD, and using this access token to retrieve the KeyVault secret.
    //
    ViewBag.Message = this.config["SecretByReference"];

    return View();
}
```
Take a moment to appreciate how much simpler this code is than the method we used previously. All the same work is still being done, but it is now being done under the covers by the Azure App Service hosting platform.
+ Under the **Security** folder, right-click **Add** > **View...** and name your view `Secret3`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>Secret By Reference</h2>
<h3>@ViewBag.Message</h3>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, add a new line for the view:
`<li><a asp-area="" asp-controller="Security" asp-action="Secret3">Secret3</a></li>`

Publish the application again and navigate to the **Secret3** web page to observe the secret retrieved from the Key Vault using the configuration reference.

### A note on this method for retrieving a secret
This method is remarkably simple from a code perspective, as the code's logic need not know anything about the KeyVault for the Managed Identity of the web app. The entire process of getting an access token and retrieving the secret is handled by the hosting platform.

Just like the second option this means that, during development, configuration and secrets can be kept in a local file on the developers machine, without risking credentials being leaked into source control.

# Lab 2 - Use Managed Identity to access SQL
In this lab we will use Managed Identity to access an Azure SQL database without having to specify a user ID or password in the connection string.

## Add the web app's Managed Identity to SQL DB
We will need the Web App Managed Identity to be recognised and authorized by the SQL database. To add it to the database we need to connect with an administrative account that has access to both Azure AD and the SQL DB.

+ In the Azure database server [**az{your_id}-security-sql-server**] goto the **Active Directory admin** blade.
+ click **Set admin**, and select your own identity
+ Click **Save**
+ Once the admin has been set goto the **AdventureWorksSample** database
+ In **Query editor (preview)** use your own identity to login (possible now because you have been set as admin)
+ Execute the following SQL script to add the web app managed identity as a user to the database, and set its permissions:
```sql
CREATE USER [az{your_id}-security-webapp] FROM EXTERNAL PROVIDER
ALTER ROLE db_datareader ADD MEMBER [az{your_id}-security-webapp] -- gives permission to read to database
ALTER ROLE db_datawriter ADD MEMBER [az{your_id}-security-webapp] -- gives permission to write to database
```

## Add the code to access SQL using Managed Identity
In Visual Studio:
+ Add the NuGet package:
  + `System.Data.SqlClient` (version 4.6.1 or later)
+ Open the **SecurityController**, and add the following method:
```cs
public async Task<ActionResult> SQL()
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
```
Again, take a moment to read and comprehend the code.

+ Under the **Security** folder, right-click **Add** > **View...** and name your view `SQL`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>SQL Connection</h2>
<h3>@ViewBag.Message</h3>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, add a new line for the view:
`<li><a asp-area="" asp-controller="Security" asp-action="SQL">SQL</a></li>`

Publish the application and observe that we have connected to Azure SQL DB using the managed identity (and retrieved the number of customers in our database). Notice that at no time has a SQL password been utilised.

**NOTE:** The connection string was already added to the web app's configuration when the web app was created.

# Lab 3 - Force web traffic through an Application Gateway
An Application Gateway may be used to provide additional protection (using the WAF features), or even to isolate the web app from the internet entirely. In this lab we will add a application gateway, and restrict access to the web app, unless accessed via the gateway.

https://docs.microsoft.com/en-us/azure/application-gateway/configure-application-gateway-with-private-frontend-ip#create-an-application-gateway

### Create an Applicaiton Gateway
+ In the portal, click on **+ Create a resource**
+ Search for _"Application Gateway"_, and click create
+ In the *Basics* tab of the Application Gateway creation blade:
  + Resource group: `az{your_id}-security-rg`
  + Application gateway name: `az{your_id}-security-appgateway`
  + Region: `West Europe`
  + Enable autoscaling: `No`
  + Leave other settings as default
  + Virtual network: click **create new**
    + Name: `az{your_id}-security-appgateway-vnet`
    + Leave the other settings default to create a vnet with a default subnet.
+ Proceed to the **Frontends** tab:
  _Here we will define the front end of the app gateway._
  + Frontends IP address type: `Public`
  + Public IP: click **Create new**
    + Name: `az{your_id}-security-appgateway-pip`
+ Proceed to the **Backends** tab:
  _Here we will define the back end of the app gateway (i.e. our web app)._
  + click **+ Add a backend pool**
    + Name: `WebAppPool`
    + Add backend pool without targetss: `No`
    + In the Target type: select **App Service**, and in the Target drop down select `az{your_id}-security-webapp`
    + click **Add**
+ Proceed to the **Configuration** tab:
  _Here we will define the logic for traffic originating at the frontend (public IP address) to be routed to the backend (our web app)._
  _This is a very complicated section as there is a significant potential for complex routing rules between multiple frontends and backends. It will likely seem overly complex for our simplistic use case, of routing 1:1._
  + click **+ Add a rule**
    + Name: `DirectRule`
    + In the Listener tab
      + Listener name: `HttpInbound`
      + Frontend IP: `Public`
      + Protocol: `HTTP`
      + Port: `80`
      + Leave other options as default.
    + In the **Backend targets** tab:
      + Backend target: `WebAppPool`
      + HTTP settings: click **Create new**
        + HTTP setting name: `HttpsOutbound`
        + Backend protocol: HTTPS
        + Port: 443
        + Use well known CA certificate: `Yes`
        + Leave the additional settings as default
        + Override with new host name: `Yes`
        + Select **Pick host name from backend target**
        + Create custom probes: `Yes`
        + Click **Add**
    + Click **Add**
+ Proceed to the **Tags** tab
+ Proceed to the **Review + create** tab
  + Click **Create**

The deployment of the application gateway will take approximately 3.5 minutes.

Once the deployment is complete, go to the application gateway's blade. In the overview you will find the frontend public IP address assigned to the gateway. Navigate to this address in your browser, this will (if everything was setup correctly) reach your web application.

At this point your web app is still available from it's own address (bypassing the gateway). We'll now restrict all traffic to our web app to route via the gateway.

+ In your web app's blade, goto **Settings** -> **Networking**
+ Under **Access Restrictions**, select **Configure Access Restrictions**
+ Notice that currently the default rule allows all traffic to the web app.
+ Click **+ Add rule**
  + Name: `AllowGateway`
  + Action: `Allow`
  + Priority: `300`
  + Type: `Virtual Network`
  + Select the virtual network we created the gateway in (`az{your_id}-security-appgateway-vnet`)
  + Select the subnet you created the gateway in (`default`)
  + click **Add rule**
+ Notice now the default rule has changed to deny all traffic, and that the rule we have just added is now the only open route into the webapp.

Now if you navigate to the web apps url (`https://az{your_id}-security-webapp.azurewebsites.net`) you will encounter a 403 error. Azure incorrectly reports that the web app is stopped, whilst infact it is merely unreachable.
However, navigating to the gateways frontend public IP will still open the web app.


### A note on gateways
In this lab we have created an application gateway with a public IP address. In this case our web app is still open to the internet, albeit only via the gateway. This would allow us to utilise the additional security and performance benefits of using an app gateway or WAF.

One can also setup the gateway with a private IP address. In this case the web app would only be accessible from inside the VNET. If the VNET has connectivity to your on-prem network then you would have created an internal-only accessible web application.

# Lab 4 - Using service endpoints to restrict access to database (for enrichment)
When creating the applicaiton gateway, and restricting access to our web app we used service endpoints to allow access from the VNET to our web app. This was setup automatically for us when we created the _Access Restriction_ rule in the web app.

We can do the same for a storage account and observe the effects of doing so.

A service endpoint requires two pieces of setup. Firstly the appropriate endpoint type must be enabled for the VNET or subnet, and secondly the resource must enable access from that VNET or subnet.

+ Return to the VNET created (`az{your_id}-security-appgateway-vnet`), and go to the **Settings** -> **Service endpoints**
  _Notice that an entry already exists for the Microsoft.Web services. This is what was added when we created the access restriction_
+ Add another entry for _Microsoft.Storage_.
+ Create a storage account (give this any unique name you choose)
+ Once the storage account has been created, add a container to the blob storage
  + In **Storage Explorer (preview)**
  + Right-click **BLOB CONTAINERS** select **Create blob container**
+ Now, setup network restrction. Go to **Settings** -> **Firewalls and virtual networks**
+ Choose **Selected networks**.
+ Click **+ Add an existing virtual network**.
  + Select the VNET and subnet as required.
+ Go to the **Storage Explorer (Preview)** and type to expand the FILE SHARES, QUEUES or TABLES. Try selecting the blob container to observe its contents.

In this case you will not be able to observe the content of the storage account. The firewall rule we just added has restricted access to only originate from our VNET.

Other resource types that can utilise service endpoints are Azure SQL SB, Cosmos DB, Event Hub, Key Vault and Service Bus (to name a few). A strong architecture will employ service endpoints to restrict access to resources and data.

## Going further

As an extension to this lab, try creating a VM in the VNET and attempt to access the storage. This is possble when inside the VNET.


# Complete
In this lab you have explored some of the security features of Azure.

You may now delete the `az{your_id}-security-rg` resource group.
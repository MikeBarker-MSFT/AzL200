

# Design for Resilience in Azure

## Clone or download the lab content
+ From clone or download the git repo

## Deploy a web app and SQL database
+ Open a PowerShell window
+ Execute the **Create-ResilienceLab** PowerShell script found in the **0_setup** folder, with your lab ID: `.\Create-ResilienceLab.ps1 -participantId {your_id}`
  + Specify a SQL user name and strong password (at least 6-characters, with uppercase, lowercase, and digits)
  + Remember, or make a note of your chosen user name and password as you will need them later.
  + This script will create a new resource group, `az{your_id}-resilience-rg`
  + The resource group will contain:
    + a database with the **AdventureWorksLT** sample;
    + an empty function app, pre-configured to point to the database; and
    + an empty web app, pre-configured to point to the function app.

The script will take approximately 1.5 - 2 min to complete.


### Publish the function app to Azure
+ Open the `Lab.Resilience.sln` file in Visual Studio. The solution contains two C# projects for (a) .NET Core function application, and (b) a .NET Core MVC web application.
+ Right-click the `FunctionApp` project, **Publish...**
+ In the pop-up, select **Select Existing** and **Create Profile**.
  + Find your web application `az{your_id}-resilience-funcapp`, and select it.
  + Click **Ok**, and then **Publish** to begin publishing the app.

### Publish the web app to Azure
+ In Visual Studio:
+ Right-click the `WebApplication` project, **Publish...**
+ In the pop-up, select **Select Existing** and **Create Profile**.
  + Find your web application `az{your_id}-resilience-webapp`, and select it.
  + Click **Ok**, and then **Publish** to begin publishing the app.
+ When the application has been published to Azure it will open in your browser.
+ Try navigating to the **All Customers** page and selecting a customer from the list.

When clicking on the customer views in the Web app, a HTTP call is made to the Function App which implements a REST API. The function app will retrieve data from the database and return this to the web app to be displayed. This represents a standard multi-tier application.

Spend a few minutes navigating around the web application to get a feel for the application, and to generate some metric data.

# Lab 1 - SQL Readonly Endpoints
Azure SQL database allows your applications to connect to a read-only endpoint. In this lab we will update our backend functions to make use of this read-only replica to offload some of the work from the primary read-write replica.

Our function app is already 'readonly' aware (see the file `SqlExecutor.cs`), and able to use different connection strings depending on whether the operation is modifying data or not.

### Enable read-replicas in the Azure SQL database
Read-only endpoints are only available on the Premium SKU. 
+ In the Portal:
+ Go to your SQL database's blade, select **Settings** > **Configure**
  + At the top of the configuration tab, select **Premium**
  + *DTUs*: `125 (P1)`
  + *Data max size*: `100MB`
  + *Read scale-out*: `Enabled`
    Notice also that there is an option to make this database zone redundant. We're not going to do so in this lab, but it is worth considering for production workloads.
  + Click **Apply**

The process of upgrading the tier of the database requires copying the data across to a new server. Therefore this process can take a few minutes depending on the database size.

**NOTE:** It is also possible to scale out the number of read replicas to more than one instance, but this is not immediately accessible through the portal and requires an ARM template deployment, or PowerShell script to do so.

### Add the read-only connection string to the Function App configuration
+ Go to your function app's blade, and select **Configuration**
+ In the **Connection strings** section you will see two connection strings already configured for your function to access the SQL database. These are currently identical.
+ Edit the **SqlConnectionStringReadOnly** connection string:
  + Append the property `ApplicationIntent=ReadOnly;` to *Value*.
    The full connection string should look something like:
    `Server=tcp:az{your_id}-resilience-sql-server.database.windows.net,1433;Initial Catalog=AdventureWorksSample;User ID={username};Password={password};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;ApplicationIntent=ReadOnly;`
  + Click **OK**
+ Click **Save** (top-left of the configuration blade) to commit the changes to the connection string.

Return to the web application and ensure that you can retrieve **All customers**, now accessing the readonly secondary replica of Azure SQL.

**NOTE:** The initial call to the function app may seem slow, this is because the function app must restart after the settings were updated. Subsequent calls will complete responsively.

# Lab 2.a - SQL Geo-replication and failover groups
In this lab we will setup geo-replication for our Azure database to the *North Europe* region, and create a failover group to allow automatic failover between the global instances.

### Create a geo-replica
+ In the portal, go to your SQL database's blade.
+ Select **Settings** > **Geo-Replication**
+ From the target region select **North Europe**
  + Because we don't have a Server available in the target region we will need to create one.
  + Select *Target Server*
    + If not already selected choose *Create a new server*
    + *Server name*: `az{your_id}-resilience-sql-secondary`
    + *Server admin login*: provide the same details as given to the setup script
    + *Password*: provide the same details as given to the setup script
    + *Confirm password*: as above
    + Click **Select**
+ Click **OK**

This will create a secondary server and database in the North Europe region, and begin copying the data in your database to this instance.    
The entire process will take approximately 4 minutes to complete.

### Use the secondary geo-replica as a read instance
Just as we did with the readonly endpoint, we can update the function app to use this secondary instance for read-only operations.

+ Go to your function app's blade, and select **Configuration**
+ In the **Connection strings** section edit the **SqlConnectionStringReadOnly** connection string:
  + Change the connection string to point to your new secondary database, i.e.
    `Server=tcp:az{your_id}-resilience-sql-secondary.database.windows.net,1433;Initial Catalog=AdventureWorksSample;User ID={username};Password={password};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;ApplicationIntent=ReadOnly;`
  + Click **OK**
+ Click **Save** (top-left of the configuration blade) to commit the changes to the connection string.

Again, return to the web application and ensure that you can retrieve **All customers**, now accessing the readonly secondary replica of Azure SQL.

This method is useful when you have globally distributed users. Data replication can be used to bring the data close to where your users are. Writes to the SQL database still need to be completed in the primary region, but reads can take place to the local secondary.
**Note:** The changes are copied asynchronously, and so there is a small delay between writing to the primary, and it being available in the secondary.

## Lab 2.b - Failover group
Now that we have geo-replication of our database let's setup a failover group on-top of that.

+ In the portal
+ Go to the *West Europe* database server's blade (i.e. `az{your_id}-resilience-sql-server`)
+ Under **Settings** select **Failover groups**
+ Click **+ Add group**
  + *Failover group name*: `az{your_id}-resilience-sql-failovergroup`
  + Click **Secondary Server, Configure required server**
    + Select the *North Europe* instance we just created `az{your_id}-resilience-sq-secondary`
  + *Read/Write failover policy*: `Manual`
  + Click **Database within the group, Select databases to add**
    + Select the **AdventureWorksSample** database.
    + Click **Select**
  + Click **Create**

We have selected Manual failover policy, rather than automatic. You may want your production apps to failover automatically to provide resilience.

### Update the function app to use the failover group endpoint.
Once the failover group has been created we can point our function app at the failover group's endpoint instead of the underlying database servers. This will allows us to failover the database without updating the applications configuration.

+ In the portal, return to your function app's blade
+ In the **Connection strings** section edit the connection strings:
  + *SqlConnectionString*: `Server=tcp:az{your_id}-resilience-sql-failovergroup.database.windows.net,1433;Initial Catalog=AdventureWorksSample;User ID={username};Password={password};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
  + *SqlConnectionStringReadOnly*: `Server=tcp:az{your_id}-resilience-sql-failovergroup.database.windows.net,1433;Initial Catalog=AdventureWorksSample;User ID={username};Password={password};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;ApplicationIntent=ReadOnly;`
+ Click **Save** (top-left of the configuration blade) to commit the changes to the connection string.

Return to your web application and confirm that the new endpoints operate as expected.

### Enact a failover
We will now enact a failvoer to switch the North Europe server to be the primary.

+ In the portal
+ Go to the *West Europe* database server's blade (i.e. `az{your_id}-resilience-sql-server`)
+ Under **Settings** select **Failover groups**
+ Select the failover group `az{your_id}-resilience-sql-failovergroup`
+ In the failover group's blade click the **Failover** button at the top.
  + Confirm **Yes** to the warning message.
    Take the time to read this warning and understand its implications. All connections to the databases will be closed, and open transactions rolled back. You may want to consider having retry logic in your application to protect against such occurances.

The failover will take approximately a minute to complete.

Once the failover has completed:
+ Notice in the failover blade that the *North Europe* server (`az{your_id}-resilience-sql-secondary`) is now the primary, and the *West Europe* is the secondary.
+ Return to your web app, and confirm that it still opperates as expected, even though the database underneath the application has changed.


# Lab 3 - Polly
[Polly](http://www.thepollyproject.org/) is an [open source](https://github.com/App-vNext/Polly) library for adding resilience and transient-fault handling to .NET projects.

In this lab we are going to use Polly to add resilience to our web app, to protect against transient outages in our backend function app.

+ In Visual Studio
+ Right-click the WebApplication project, **Manage NuGet Packages...**
+ In NuGet, on the **Browse** tab, search for **Polly**, and install it
+ Accept the dialogues that appear.
+ Within the **Services** > **CustomerService.cs**, and update the `GetAllCustomersAsync` method:
```cs
public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
{
    string url = "api/customer";

    AsyncPolicy retryPolicy = Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) });

    HttpResponseMessage response = await retryPolicy
        .ExecuteAsync(async () =>
        {
            HttpResponseMessage resp = await this.client.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            return resp;
        });

    string responseBody = await response.Content.ReadAsStringAsync();

    var customers = JsonConvert.DeserializeObject<List<Customer>>(responseBody);

    return customers;
}
```

Take a moment to understand this logic.  We've defined a Polly policy which, when it catches an `HttpRequestException`, will wait and retry the HTTP method again. The wait will back-off for 0, 2 and 5 seconds between successive failures.

Try running the web application locally. (**HINT:** you will need to update your logic apps base URL and function key in `appsettings.Development.json`, using the same settings as the deployed web app).

Try stopping the function app and observing the behaviour of the `GetAllCustomersAsync` method.

This declarative approach to defining policies is very powerful, as we can declare multiple policies and chain them together.
e.g.
```cs
public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
{
    string url = "api/customer";

    AsyncPolicy retryPolicy = Policy
       .Handle<HttpRequestException>()
       .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) });

    AsyncPolicy circuitBreakerPolicy = Policy
       .Handle<HttpRequestException>()
       .CircuitBreakerAsync(10, TimeSpan.FromSeconds(30));

    AsyncPolicy executionPolicy = Policy.WrapAsync(
        retryPolicy,
        circuitBreakerPolicy);

    HttpResponseMessage response = await executionPolicy
            .ExecuteAsync(async () =>
            {
                HttpResponseMessage resp = await this.client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                return resp;
            });

    string responseBody = await response.Content.ReadAsStringAsync();

    var customers = JsonConvert.DeserializeObject<List<Customer>>(responseBody);

    return customers;
}
```

Now we have introduced an execution policy which incorporates the circuit breaker and retry patterns.
Again try running the web app with this logic.

# Going further
Try adding the execution policy to the other methods in the `CustomerService` class.

Notice that the policies can be extracted from the local method, and made members of the class. This allows the policies to be reused in multiple places.
 
Experiment with the overloads of the policy factory methods. For example the RetryPolicy can accept an `Action` which will be called each time the underlying method is retried.
Add logging to the retry policy each time a retry occurs.
Use the same logic to add logging to the circuit breaker policy to track the state of the circuit breaker (closed, open).

Consider the implications of switching the wrap order of the policies.

Examine the other policies available to you in the Polly library.

We've added resilience logic to the web app when calling the function app.
Add similar logic to the function app when calling the database. (Notice, you will not be handling `HttpRequestException`s when accessing the DB).
Consider implementing logic to handle only [those SQL errors which are considered transient](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-develop-error-messages).


# Complete
In this lab you have explored some of the resilience features of Azure, and design patterns to assist this.

You may now delete the `az{your_id}-resilience-rg` resource group.

# .NET Caching with Redis Lab

## Clone or download the lab content
+ From clone or download the git repo

## Deploy a web app and SQL database
+ Open a PowerShell window
+ Execute the **Create-CacheLab** PowerShell script found in the **0_setup** folder, with your lab ID: `.\Create-CacheLab.ps1 -participantId {your_id}`
  + Specify a SQL user name and strong password (at least 6-characters, with uppercase, lowercase, and digits)
  +  Remember, or make a note of your chosen user name and password as you will need them later.
  + This will create a new resource group, `az{your_id}-cache-rg`
  + The resource group will contain a database with the **AdventureWorksLT** sample.

# Lab 1.a - Redis
In this lab we will create an instance of Azure cache for Redis, and explorer how to store data in, and retrieve data from the cache.

### Create a Redis cache
+ In the Azure portal, click on "**+ Create a resource**" and search for "Azure Cache for Redis", to begin creating an Redis cache.
+ Supply the following details for the Redis instance:
  + *DNS name*: `az{your_id}-cache-redis`
  + *Resource group*: `az{your_id}-cache-rg`
  + *Region*: `West Europe`
  + *Pricing tier*: `Basic C0`
+ Leave all other defaults and **Create** the Redis cache.

The Redis cache takes approx. 10-15 min to be created. Whilst it is being created, proceed to the next steps.

### Set and get data from Redis in code 
+ Open the `Lab.Cache.sln` file in Visual Studio. The solution contains a single C# project for a .NET Core MVC web application.
+ Build the project to ensure it builds corrects.
+ Add the following NuGet packages to the project:
  + `StackExchange.Redis`
    This library is a 3rd-party client Redis SDK written for .NET, and is one of the leading .NET Redis client implementations.
+ Add a new MVC controller. Under the **Controllers** folder, right-click **Add** > **Controller...** > **MVC Controller - Empty**. Name your controller `RedisController`.
+ Paste the following code as the class definition:
```cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace RedisWeb.Controllers
{
    public class RedisController : Controller
    {
        private readonly IConfiguration configuration;
        private ConnectionMultiplexer redisConnectionMultiplexer;

        public RedisController(
            IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private ConnectionMultiplexer RedisConnectionMultiplexer
        {
            get
            {
                //
                // Establish the connection to Redis once and reuse it throughout the application.
                //
                if (redisConnectionMultiplexer == null)
                {
                    string connectionString = this.configuration.GetConnectionString("RedisConnectionString");

                    redisConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                }

                return redisConnectionMultiplexer;
            }
        }


        public async Task<IActionResult> Read()
        {
            //
            // Retrieve a string with the key "MyFirstKey" from the Redis cache.
            //
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            string myFirstCachedValue = await redisDatabase.StringGetAsync("MyFirstKey");

            ViewBag.Message = myFirstCachedValue ?? "- cache miss -";
            return View();
        }

        public IActionResult Update()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Update(string cacheValue)
        {
            //
            // Set the string with the key "MyFirstKey" in the Redis cache, specifying a TTL of 20 seconds.
            //
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            await redisDatabase.StringSetAsync("MyFirstKey", cacheValue, TimeSpan.FromSeconds(20));

            return RedirectToAction("Read");
        }

    }
}
```
Take a moment to read the code and understand what each line is doing to access the Redis cache. Notice that the connection is only established once in the `ConnectionMultiplexer`, and then reused throughout the code. This is the recommended approach when establishing a connection to Redis as it is a relatively expensive operation. Even though the `ConnectionMultiplexer` implements `IDisposable` you should not wrap the connection in using statement.

Notice that we are setting the string in the cache with a time-to-live (TTL) of 20 seconds.

+ Add a new MVC view. Under the **Views** folder, create a sub folder named `Redis`
+ Under the **Redis** folder, right-click **Add** > **View...** and name your view `Read`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>Read from cache</h2>
<h3>@ViewBag.Message</h3>
```
+ Add a second view under the **Redis** folder, right-click **Add** > **View...** and name your view `Update`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
@{
    string cacheValue = "";
}

<h2>Update cache</h2>

<form asp-action="Update" asp-controller="Redis" method="post">
    <input asp-for="@cacheValue" />

    <button type="submit">Save</button>
</form>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, find the line:
`<!-- Insert navigation links here -->`
Under this, add two new lines for the new views:
```html
<li><a asp-area="" asp-controller="Redis" asp-action="Read">Read from cache</a></li>
<li><a asp-area="" asp-controller="Redis" asp-action="Update">Update cache</a></li>
```

### Retrieve and set the Redis connection string
Once the Redis cache has been created...

+ In the Azure portal, go to the blade for your Redis instance
  + Under **Settings** -> **Access keys**, copy the **Primary connection string**
+ In Visual Studio
  + Right-click the project, and select **"Manage user secrets"**
  + In the JSON user secret file paste the following, replacing with your Redis connection string as appropriate:
```
{
  "ConnectionStrings": {
    "RedisConnectionString": "{your redis connection string}"
  }
}
```

### Run the application
+ Run the web application locally.
+ Visit the **Read from cache** link, you will initially notice a cache miss.
+ Visit the **Update cache** link, and enter some string (e.g. _"Hello world"_). When you click _Save_ the value is written to the Redis cache and you are redirected to the **Read from cache** page where you can see the cache value.
+ Further updates to the value will be set in the cache and reflected in the **Read from cache** page.
+ 20 seconds after the value has been written notice that you will begin receiving cache misses again.

We have used Redis to store and retrieve a string. However, the Redis "String" is better thought of as any byte array, rather than a string necessarily. Data are are often stored in Redis as strings (e.g. serialized JSON), but one need not be restricted to do so.

# Lab 1.b - Storing more than strings in Redis

We will now explore storing byte arrays.

### Get and set a byte array in code 
+ In the **RedisController** paste the following code:
```cs
public async Task<IActionResult> ByteArray()
{
    byte[] buffer = GenerateRandomByteArray();

    IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

    // Set the byte array in Redis
    await redisDatabase.StringSetAsync("ByteArrayKey", buffer, TimeSpan.FromSeconds(20));

    // Retrieve the byte array from Redis
    byte[] retrievedArray = await redisDatabase.StringGetAsync("ByteArrayKey");

    ViewBag.ByteArray = retrievedArray;
    return View();
}

private static readonly Random random = new Random();

private static byte[] GenerateRandomByteArray()
{
    int length = random.Next(10, 50);
    byte[] buffer = new byte[length];
    random.NextBytes(buffer);

    return buffer;
}
```
Take a moment and read the code. Notice this time we are not storing C# strings, but rather native byte arrays.

+ Create another view under the **Redis** folder, right-click **Add** > **View...** and name your view `ByteArray`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>Byte array</h2>
<h3>@BitConverter.ToString((byte[])ViewBag.ByteArray).Replace("-", " ")</h3>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, add the line:
`<li><a asp-area="" asp-controller="Redis" asp-action="ByteArray">Byte Array</a></li>`

The `GenerateRandomByteArray` will create a random byte array, and we store this in the Redis cache. We then immediately retrieve the byte array from the cache. The view simply displays this byte array as a hex formatted string.

Execute the code and navigate to the **Byte Array** page to exercise the code.

When storing large amounts of data, or when string serialisation becomes a bottle neck you might want to consider using byte arrays as these can reduce size and increase serialisation speed.

# Lab 1.c - Redis Lists
Redis can store Strings, Lists, Sets, and Geo-spatial data. We've already explored strings, but will now explore the list.

A Redis list is an ordered array of values stored under against a key. Each item in the list can be uniquely accessed using a combination of key and index. Redis provides several operations against lists; such as insert, push and pop.

### Get and set a byte array in code 
+ In the **RedisController** add the following code:
```cs
public async Task<IActionResult> List(string popValue)
{
    IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

    // Read the full list
    // Notice that -1 means read to the final index in the list
    var list = await redisDatabase.ListRangeAsync("ListKey", 0, -1);

    ViewBag.PopValue = popValue;
    ViewBag.List = list.ToStringArray();
    return View();
}

[HttpPost]
public async Task<IActionResult> Pop()
{
    IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

    // Pop a value off the list
    string popValue = await redisDatabase.ListLeftPopAsync("ListKey");

    //
    // TODO: Do some processing with the value
    //

    return RedirectToAction("List", new { popValue = popValue });
}

[HttpPost]
public async Task<IActionResult> Push(string pushValue)
{
    if (!string.IsNullOrEmpty(pushValue))
    {
        IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

        // Push a value to the list
        await redisDatabase.ListRightPushAsync("ListKey", pushValue);
    }

    return RedirectToAction("List");
}
```
Take a moment to read this code an understand what it is doing.

+ Create another view under the **Redis** folder, right-click **Add** > **View...** and name your view `List`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
@{
    System.Random random = new System.Random();
    string pushValue = random.Next(0, 100).ToString();
}

<h2>List</h2>
@if (!string.IsNullOrEmpty(ViewBag.PopValue))
{
<h3>Popped: @ViewBag.PopValue</h3>
}

<form asp-action="Pop" asp-controller="Redis" method="post">
    <button type="submit">Pop</button>
</form>

@foreach(string item in ViewBag.List)
{
    <p>@item</p>
}

<form asp-action="Push" asp-controller="Redis" method="post">
    <input asp-for="@pushValue" autofocus/>

    <button type="submit">Push</button>
</form>
```
+ Add a menu item to access the new view. Under the folder **Views** > **Shared** > **_Layout.cshtml**, add the line:
`<li><a asp-area="" asp-controller="Redis" asp-action="List">List</a></li>`

Execute the code and navigate to the **List** page. Try pushing and popping values to the list.

Notice that we've created a queue (first in, first out), but pushing and popping left and right respectively. If one were to push and pop from the same side of the list one can create a stack (last in, first out). Try this for yourself.

## Going further with Redis
As mentioned, Redis also supports Sets, and Geo-spatial data. 

Furthermore, Redis is able to store integer values and execute increment and decrement operations on them without needing to retrieve the value to the application. See the `StringDecrementAsync` and `StringIncrementAsync` methods.

It is worth mentioning that the get and set operations on strings can also work with batches, not just a single item at a time.

Consider exploring and experimenting with these features, and other features, yourself.

# Lab 2 - An abstraction for caching

| **NB**: This lab demonstrates features which require .NET Core 2.2 or later on your development machine. If you do not have this version of .NET Core, or lack the time (or inclination) to install it, skip this lab and proceed with Lab 4. |
| --- |

.NET Core provides an interface IDistributedCache which abstracts away much of the detail of the underlying cache model, and allows different caches to be substituted using dependency injection.

You can read about the IDistributedCache interface here:
https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-2.2


## Install .NET Core 2.2
If you do not have .NET Core 2.2 setup on your machine you can download it from here:
https://dotnet.microsoft.com/download
You will need the .NET Core SDK download.

## Upgrade the project to use .NET Core 2.2
+ In Visual Studio:
  + Right-click the project and go to **Properties**
  + In the new tab select .NET Core 2.2 in the **Target framework**
  + In NuGet package manager upgrade the `Microsoft.AspNetCore.Razor.Design` and `Microsoft.VisualStudio.Web.CodeGeneration.Design` dependencies to versions for 2.2


## .NET Core IDistributedCache
First lets configure the .net pipeline to build IDistributedCache to use our Redis cache:
+ Add the following NuGet packages to the project:
  + `Microsoft.Extensions.Caching.StackExchangeRedis`
    Take notice that other _"Microsoft.Extensions.Caching..."_ extensions exist to give different backing stores to IDistributedCache.
+ In the **Startup.cs** locate the **ConfigureServices** method.
+ Add the following lines:
```cs
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = this.Configuration.GetConnectionString("RedisConnectionString");
    options.InstanceName = "master";
});
```
+ Add a new MVC controller. Under the **Controllers** folder, right-click **Add** > **Controller...** > **MVC Controller - Empty**. Name your controller `CacheController`.
+ Paste the following code as the class definition:
```cs
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApplication.Controllers
{
    public class CacheController : Controller
    {
        private readonly IDistributedCache cache;

        public CacheController(
            IDistributedCache cache)
        {
            this.cache = cache;
        }

        public async Task<IActionResult> Read()
        {
            // Read from the cache
            byte[] buffer = await cache.GetAsync("DistributedCacheKey");

            string message = null;
            if (buffer != null)
            {
                message = Encoding.UTF8.GetString(buffer);
            }

            ViewBag.Message = message ?? "- cache miss -";
            return View();
        }

        public IActionResult Update()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Update(string cacheValue)
        {
            var buffer = Encoding.UTF8.GetBytes(cacheValue);

            // Set a TTL of 20 seconds
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20)
            };

            // Write to the cache
            await cache.SetAsync("DistributedCacheKey", buffer, options);

            return RedirectToAction("Read");
        }
    }
}
```
Take a moment to read the code and understand what each line is doing. Explore the methods available on the `IDistributedCache` interface. As you may expect, this abstraction is not as rich as the native Redis SDK.

+ Add a new MVC view. Under the **Views** folder, create a sub folder named `Cache`
+ Under the **Cache** folder, right-click **Add** > **View...** and name your view `Read`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
<h2>Read from cache</h2>
<h3>@ViewBag.Message</h3>
```
+ Add a second view under the **Cache** folder, right-click **Add** > **View...** and name your view `Update`. (Leave other defaults and click Add)
+ Paste the following code as the view:
```html
@{
    string cacheValue = "";
}

<h2>Update cache</h2>

<form asp-action="Update" asp-controller="Cache" method="post">
    <input asp-for="@cacheValue" />

    <button type="submit">Save</button>
</form>
```
+ In **Views** > **Shared** > **_Layout.cshtml**, remove the references to the **Redis** views to prevent the navigation menu getting cramped.
+ Add the following lines for the **Cache** views:
```html
<li><a asp-area="" asp-controller="Cache" asp-action="Read">Read from cache</a></li>
<li><a asp-area="" asp-controller="Cache" asp-action="Update">Update cache</a></li>
```

Run the application and view the cache miss in the **Read** page. Update the value, in the **Update cache** page, and notice the value is reflected in the **Read** page.

**Additional exercise**: Consider trying to prove this value is indeed being retrieved from Redis.
**Hint:** The cached value is stored in the Redis cache with the key `masterDistributedCacheKey`, the concatenated InstanceName configuration value from StartUp.cs and key supplied in CacheController.cs.

#### Going further with IDistributedCache
It is useful to be able to develop without requiring each developer to have their own instance of Redis.

Notice that in StartUp.cs we could have configured the services to use a local in-memory cache instead of Redis for IDistributedCache.  This is done using the `services.AddDistributedMemoryCache()` instead of the `services.AddStackExchangeRedisCache()` extension method.

A conditional statement can optionally switch between using Redis or local memory depending on the value of `CurrentEnvironment`.
Read more about this here:
https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-2.2


# Lab 3 - User Sessions with Redis in .NET Core
We can use Redis to store the users' session state. This allows multiple instance of your application to share the session state which means your app can scale-out without requiring sticky sessions.

The .NET Core Sessions object builds on-top of the default distributed cache object, `IDistributedCache`, which we explored in the previous lab.  Configuring this interface to use Redis then also configures the backing store for sessions.

| Note: A different method to use Redis for sessions also exists for [.NET Framework](https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-aspnet-session-state-provider) |
| --- |


+ In **StartUp.cs** in the `ConfigureServices` method add the following line to add sessions as a service...
```cs
services.AddSession();
```
+ ...and in the `Configure` method add the following line to instruct .NET Core to utilise session state
```cs
app.UseSession();
```
+ Add the following method to the **CacheController**.
```cs
public async Task<IActionResult> SessionState()
{
    // Get the session data
    await HttpContext.Session.LoadAsync();

    string sessionTime = HttpContext.Session.GetString("SessionTime");
    if (sessionTime == null)
    {
        sessionTime = DateTime.Now.ToString("hh:mm:ss.fff");

        // Store the session time in
        HttpContext.Session.SetString("SessionTime", sessionTime);

        await HttpContext.Session.CommitAsync();
    }

    ViewBag.Message = sessionTime;
    return View();
}
```
This will require adding a using statement `using Microsoft.AspNetCore.Http;`
+ Add a view for **SessionState** to the Cache views folder:
```html
<h2>Read from cache</h2>
<h3>Time from Sessions: @ViewBag.Message</h3>
```
+ In **Views** > **Shared** > **_Layout.cshtml**, add a line to navigate to the SessionState view:
```html
<li><a asp-area="" asp-controller="Cache" asp-action="SessionState">Session State</a></li>
```

Run the application and notice that your session state is being used to capture and retrieve the start time of your session.

This illustrates several mechanisms for utilise Redis as a shared state provider across multiple instances of an application. In a real-world environment you may wish to utilise any of these methods: from Native SDK, to IDistributedCache, to user Session state. Which is best will depend on the application and development environment.


# Lab 4 - Using Redis and SQL DB in your application.
Code already exists in the application to get and edit customers. An intentional delay has been introduced into the database accessor to simulate a slow-responding remote call.

In this lab we will explore how to improve application performance (and reduce database load) by using Redis as a cache.

### Prepare the environment
+ From the Azure portal go to the database **AdventureWorksSample** on the Azure SQL server **az{your_id}-cache-sql-server**.
  + Under **Settings** -> **Connection strings** copy the ADO.NET connection string.
+ Within Visual Studio: In the user secrets file (right-click the project **Managed User Secrets**), add a new connection string for your SQL database:
```cs
{
  "ConnectionStrings": {
    "RedisConnectionString": "{your redis connection string}",
    "SqlConnectionString": "{your sql connection string}"
  }
}
```
Remember to substitute your SQL username and password provided when you ran the setup script.
+ In **Startup.cs**, add the following line to the `ConfigureServices` method.
```cs
services.AddSingleton<ICustomerService, DbCustomerService>();
```
+ In **Views** > **Shared** > **_Layout.cshtml**, remove the references to the **Redis** and **Cache** views to prevent the navigation menu getting cramped.
+ Add the following line to light-up the **Customer** view:
```html
<li><a asp-area="" asp-controller="Customer" asp-action="Index">All Customers</a></li>
```

Run the application and navigate to the **All customers** page. Select one of your customers and edit a property then click Save. Notice that because of the delays we've introduced into the `DbCustomerService`, the application feels sluggish and this is an unpleasant experience for the user.

### Create a Redis ICustomerService implementation
+ Add the following **RedisCustomerService** class in the same folder as **DbCustomerService** (i.e. **Services**):
```cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public class RedisCustomerService : ICustomerService
    {
        private readonly IConfiguration configuration;
        private ConnectionMultiplexer redisConnectionMultiplexer;

        public RedisCustomerService(
            IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        private ConnectionMultiplexer RedisConnectionMultiplexer
        {
            get
            {
                if (redisConnectionMultiplexer == null)
                {
                    string connectionString = this.configuration.GetConnectionString("RedisConnectionString");

                    redisConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                }

                return redisConnectionMultiplexer;
            }
        }


        Task<IEnumerable<Customer>> ICustomerService.GetAllCustomersAsync()
        {
            throw new NotSupportedException();
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            string key = GetCustomerKey(customerId);

            string customerJson = await redisDatabase.StringGetAsync(key);

            Customer customer = null;
            if (customerJson != null)
            {
                customer = JsonConvert.DeserializeObject<Customer>(customerJson);
            }

            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            string customerJson = JsonConvert.SerializeObject(customer);

            string key = GetCustomerKey(customer.CustomerID);

            await redisDatabase.StringSetAsync(key, customerJson);
        }

        private static string GetCustomerKey(long customerId)
        {
            return $"Customer_{customerId}";
        }

        public async Task CacheCustomers(IEnumerable<Customer> customers)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            var pairs = customers
                .Select(customer =>
                {
                    string customerJson = JsonConvert.SerializeObject(customer);
                    string key = GetCustomerKey(customer.CustomerID);

                    return new KeyValuePair<RedisKey, RedisValue>(key, customerJson);
                })
                .ToArray();

            await redisDatabase.StringSetAsync(pairs);
        }
    }
}
```

+ Add another implementation of ICustomerService, named **CustomerService**, which combines the Redis and database implementations

```cs
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly DbCustomerService dbService;
        private readonly RedisCustomerService redisService;

        public CustomerService(
            IConfiguration configuration)
        {
            this.dbService = new DbCustomerService(configuration);
            this.redisService = new RedisCustomerService(configuration);
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var result = await this.dbService.GetAllCustomersAsync();

            return result;
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            var customer = await this.redisService.GetCustomerAsync(customerId);

            if (customer == null)
            {
                customer = await this.dbService.GetCustomerAsync(customerId);
                await redisService.CacheCustomers(new[] { customer });
            }

            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            // Write to both the DB and Redis services
            var dbpdateTask = this.dbService.UpdateCustomerAsync(customer);
            var redisUpdateTask = this.redisService.UpdateCustomerAsync(customer);

            await redisUpdateTask;
            await dbpdateTask;
        }
    }
}
```
+ Update the **Startup.cs** class to use `CustomerService` instead of `DbCustomerService` within the dependency injection configuration.

Execute the application again. The application is more responsive now when repeatedly editing a customer (although loading the initial list is still slow).

Here we are using a combination of the cache-aside, and write-through-cache patterns to improve performance of our application.

# Going further
Try to improve the application responsiveness further by implementing the `GetAllCustomersAsync` method of **RedisCustomerService**.

Try improving performance further by improving the users experience on updates.
**Hint:** Do you need to wait for the DB write to complete before return from the update method?

In a production environment you want your application to be resilient to failures. How could you handle a failure during an asynchronous DB write in a production system?


# Complete
In this lab you have explored some of the cache features of Azure.

You may now delete the `az{your_id}-cache-rg` resource group.
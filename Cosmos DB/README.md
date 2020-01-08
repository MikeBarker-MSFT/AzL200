# Workshop-CosmosDB

## Exercise 1 - Create CosmosDB Account
You can refer ["Microsoft Docs - Create an Azure Cosmos account, container, and items"](https://docs.microsoft.com/en-us/azure/cosmos-db/create-cosmosdb-resources-portal) for excercise 1.

### Steps
In this step we will create a Cosmos DB database using the SQL API, and add a collection to the account.

In the Azure portal...
1. Create an Azure Cosmos DB account. Click on **+ Create a resource** and search for `Azure Cosmos DB`. Click **Create**.
    * *Resource Group:* **Create new**
      * *Name:* `az{your_id}-cosmosdb-rg`
    * *Account Name*: `az{your_id}-cosmosdb`
    * *API*: `Core (SQL)`
    * *Apache Spark*: `None`
    * *Location*: `West Europe`
    * *Geo-Redundancy*: `Disabled`
    * *Multi-region Writes*: `Disabled`
    **NOTE:** The database account will take approx. 10 minutes to be created.
2. Create a database in the Cosmos DB account. In the CosmosDB's select **Data Explorer** to open the data explorer blade.
    * From the drop dwon at the top of the blade, select **New Database**
    * *Database Id*: `ToDoList`
    * *Provision throughput*: `Enabled`
    * *Throughput*: `400`
3. Add a container. From the drop dwon at the top of the blade, select **New Container**
    * *Database Id*: **Use Existing** `ToDoList`
    * *Container Id*: `Items`
    * *Partition Key*: `/id`
4. Add data to your container.
    * Select the `ToDoList` container created, and from the menu at the top of the blade click **New Item**.
    * Paste the json snippet below, which represents a new item in our todo list.
    ```json
    {
        "id": "BD739417-E69A-41FB-BF7C-7C4D7D487B28",
        "category": "personal",
        "name": "groceries",
        "description": "Pick up apples and strawberries.",
        "isComplete": false
    }
    ```
    * Click **Save**
    
**Note:** After saving the document you will notice several fields have been automatically added by Cosmos DB. `_rid`, `_self`, `_etag`, `_attachments`, and `_ts`.
You can read more about these tags here: https://docs.microsoft.com/en-us/rest/api/cosmos-db/collections

## Exercise 2 - Create a blank ASP.NET Core MVC application
You can refer ["Microsoft Docs - Get started with ASP.NET Core MVC"](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/start-mvc) for excercise 2.

## Exercise 3 - Set up the ASP.NET Core MVC application

### Steps

1. Add Azure Cosmos DB .NET NuGet package to your project
    * Right-click your MVC project, **Manage NuGetPackages...**
    * In the NuGet window search for `Microsoft.Azure.Cosmos`, and install
2. Add a model class Item.cs
    ```cs
    using Newtonsoft.Json;

    namespace Models
    {
        public class Item
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "category")]
            public string Category { get; set; }

            [JsonProperty(PropertyName = "description")]
            public string Description { get; set; }

            [JsonProperty(PropertyName = "isComplete")]
            public bool Completed { get; set; }
        }
    }
    ```
3. Add below views under Views Folder
    * [Index](./Views/Index.cshtml)
    * [Create](./Views/Create.cshtml)
    * [Edit](./Views/Edit.cshtml)
    * [Details](./Views/Details.cshtml)
    * [Delete](./Views/Delete.cshtml)
    
4. Add a new Controller under Controllers folder
    * Name : ItemController
    * MVC Controller - Empty
    
5. Replace the contents of **ItemController.cs** with the following code:
    ```cs
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Models;

    public class ItemController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        public ItemController(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _cosmosDbService.GetItemsAsync("SELECT * FROM c"));
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Name,Category,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                item.Id = Guid.NewGuid().ToString();
                await _cosmosDbService.AddItemAsync(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind("Id,Name,Category,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await _cosmosDbService.UpdateItemAsync(item.Id, item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            Item item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            Item item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("Id")] string id)
        {
            await _cosmosDbService.DeleteItemAsync(id);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            return View(await _cosmosDbService.GetItemAsync(id));
        }
    }
    ```
## Exercise 4 - Connect to Azure Cosmos DB

### Steps

1. Add a new Folder "Services" under your project.
2. Add a new class "ICosmosDBService.cs" under Services folder.
3. Replace the contents of ICosmosDBService.cs with the following code:
    ```cs
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Models;

    public interface ICosmosDbService
    {
        Task<IEnumerable<Item>> GetItemsAsync(string query);
        Task<Item> GetItemAsync(string id);
        Task AddItemAsync(Item item);
        Task UpdateItemAsync(string id, Item item);
        Task DeleteItemAsync(string id);
    }
    ```
    
4. Add a new class "CosmosDBService.cs" under Services Folder, and replace it's contents with the following code:
    ```cs
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.Extensions.Configuration;

    public class CosmosDbService : ICosmosDbService
    {
        private Container _container;

        public CosmosDbService(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(Item item)
        {
            await this._container.CreateItemAsync<Item>(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await this._container.DeleteItemAsync<Item>(id, new PartitionKey(id));
        }

        public async Task<Item> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Item> response = await this._container.ReadItemAsync<Item>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<Item>> GetItemsAsync(string queryString)
        {
            var query = this._container.GetItemQueryIterator<Item>(new QueryDefinition(queryString));
            List<Item> results = new List<Item>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task UpdateItemAsync(string id, Item item)
        {
            await this._container.UpsertItemAsync<Item>(item, new PartitionKey(id));
        }


        /// <summary>
        /// Creates a Cosmos DB database and a container with the specified partition key. 
        /// </summary>
        public static CosmosDbService Initialize(IConfigurationSection configurationSection)
        {
            return InitializeAsync(configurationSection)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Creates a Cosmos DB database and a container with the specified partition key. 
        /// </summary>
        public static async Task<CosmosDbService> InitializeAsync(IConfigurationSection configurationSection)
        {
            string databaseName = configurationSection.GetSection("DatabaseName").Value;
            string containerName = configurationSection.GetSection("ContainerName").Value;
            string account = configurationSection.GetSection("Account").Value;
            string key = configurationSection.GetSection("Key").Value;

            CosmosClientBuilder clientBuilder = new CosmosClientBuilder(account, key);
            CosmosClient client = clientBuilder
                                .WithConnectionModeDirect()
                                .Build();

            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
            DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            return cosmosDbService;
        }
    }
    ```

5. Within the **Startup.cs** class, add the following line to the `ConfigureServices` method:
   ```cs
   services.AddSingleton<ICosmosDbService>(CosmosDbService.Initialize(Configuration.GetSection("CosmosDb")));
   ```

6. Define the configuration in Secret file.
    * Right click on project and click on **Manage User Secrets** and add a section called CosmosDb:
    ```json
    {
        "CosmosDb": {
          "Account": "<enter the URI from the Keys blade of the Azure Portal>",
          "Key": "<enter the PRIMARY KEY, or the SECONDARY KEY, from the Keys blade of the Azure  Portal>",
          "DatabaseName": "ToDoList",
          "ContainerName": "Items"
        }
    }
    ```
    * Replace the account URI and key from the values in the portal. In the Azure portal, in your Cosmos DB accounts's blade:
      * Navigate to **Settings** -> **Keys**
      * In the **Read-write Keys** tabs...
      * Copy the **URI** (`https://az{your_id}-cosmosdb.documents.azure.com:443/`), and paste this into the **User Serects** section in Visual Studio.
      * Copy the **PRIMARY KEY**, and paste this into the **User Serects** section in Visual Studio. 

7. Build and run the your MVC application.
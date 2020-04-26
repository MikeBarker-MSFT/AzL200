# Workshop-AzureStorage

## Exercise 1 - Create Azure Storage Account
In this exercise we will use the Azure portal to create an Azure storage account, create an azure table and add data to the storage table.

You can refer ["Quickstart: Create an Azure Storage table in the Azure portal"](https://docs.microsoft.com/en-us/azure/storage/tables/table-storage-quickstart-portal) for this excercise.

### Steps

In the Azure portal...
1. Create an Azure Storage account. 
    * Click on **+ Create a resource** and search for `Storage Account`. Click **Create**.
    * Select below options while creating storage account
      * *Resource Group:* **Create new**
         * *Name:* `az{your_id}-storageaccount-rg`
      * *Storage account name*: `az{your_id}-storageaccount`
      * *Location*: `West Europe`
      * *Performance*: `Standard`
      * *Account kind*: `StorageV2`
      * *Replication*: `RA-GRS`
      * *Access tier*: `Hot`
    * Click on **Review and Create**
    
2. Create a storage table in the Azure storage account. 
    * In the Storage Account select **Storage Explorer** to open the storage explorer blade.
    * Right click on `Tables` from explorer blade and select `Create Table`
    * Enter value for `Table Name` as `storagedemotable` and Click `Ok`
    * Now you can see `storagedemotable` under `Tables` node in explorer blade.
        
3. Add data to your table.
    * Select create table `storagedemotable` and from the menu at the top of the blade click **+ Add**.
    * Use below values for PartitionKey and RowKey
      * *PartitionKey*: `2019`
      * *RowKey*: `04cf3b2a-6cc6-42c4-b1df-b34db6429002`
    * You can use any `GUID` value for RowKey
    * Add a new property by clicking on **Add Property** and Use below values:
      * *PropertyName*: `Name`
      * *Type*: `String`    
      * *Value*: `groceries`
    * Add a new property by clicking on **Add Property** and Use below values:
      * *PropertyName*: `Description`
      * *Type*: `String`    
      * *Value*: `Pick up apples and strawberries`
    * Add a new property by clicking on **Add Property** and Use below values:
      * *PropertyName*: `Completed`
      * *Type*: `Boolean`    
      * *Value*: `false`
    * Add a new property by clicking on **Add Property** and Use below values:
      * *PropertyName*: `category`
      * *Type*: `String`    
      * *Value*: `Personal`  
    * Click **Insert** at the bottom of the page.
      
You can read more about table properties [here](https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model)

## Exercise 2 - Create a blank ASP.NET Core MVC application
In this exercise we will create a ASP.NET Core MVC application with controllers and views.

You can refer ["Microsoft Docs - Get started with ASP.NET Core MVC"](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/start-mvc) for this excercise.

## Exercise 3 - Set up the ASP.NET Core MVC application

### Steps
Now that we have most of the ASP.NET Core MVC code that we need for this solution, let's add the NuGet packages required to connect to Azure Storage Account.

1. Add Azure Storage Account .NET NuGet package to your project
    * Right-click your MVC project, **Manage NuGetPackages...**
    * In the NuGet window search for `WindowsAzure.Storage` and install
2. Under Models folder add a new class `ToDoItem.cs`
    ```cs
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    public class ToDoItem : TableEntity
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "isComplete")]
        public bool Completed { get; set; }
    }
    ```
3. Under Models folder add a new class `AzureTableSettings.cs`
    ```cs
    using System;
    public class AzureTableSettings
    {
        public AzureTableSettings(string storageAccount,
                                       string storageKey,
                                       string tableName)
        {
            if (string.IsNullOrEmpty(storageAccount))
                throw new ArgumentNullException("StorageAccount");

            if (string.IsNullOrEmpty(storageKey))
                throw new ArgumentNullException("StorageKey");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("TableName");

            this.StorageAccount = storageAccount;
            this.StorageKey = storageKey;
            this.TableName = tableName;
        }

        public string StorageAccount { get; }
        public string StorageKey { get; }
        public string TableName { get; }
    }
    ```
    
4. Add Views
   * Create a new `Item` folder under `Views` folder.
   * Add below views under `Item` folder.
       * [Index](./Views/Index.cshtml)
       * [Create](./Views/Create.cshtml)
       * [Edit](./Views/Edit.cshtml)
       * [Details](./Views/Details.cshtml)
       * [Delete](./Views/Delete.cshtml)
    
5. Add a new Controller under Controllers folder
    * Name : ItemController
    * MVC Controller - Empty
    
6. Replace the contents of **ItemController.cs** with the following code:
    ```cs
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Services;

    public class ItemController : Controller
    {
        private readonly IToDoItemService _service;
        public ItemController(IToDoItemService toDoService)
        {
            _service = toDoService;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _service.GetToDoList());
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Name,Category,Description,Completed")] ToDoItem item)
        {
            if (ModelState.IsValid)
            {
                item.RowKey = Guid.NewGuid().ToString();
                item.PartitionKey = DateTime.UtcNow.Year.ToString();
                await _service.AddToDoItem(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind("Name,Category,Description,Completed,RowKey,PartitionKey")] ToDoItem item)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateToDoItem(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string partitionKey, string rowKey)
        {
            if (rowKey == null)
            {
                return BadRequest();
            }

            ToDoItem item = await _service.GetDoToItem(partitionKey, rowKey);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string partitionKey, string rowKey)
        {
            if (rowKey == null)
            {
                return BadRequest();
            }

            ToDoItem item = await _service.GetDoToItem(partitionKey, rowKey);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("PartitionKey")] string partitionKey, [Bind("RowKey")] string rowKey)
        {
            await _service.DeleteToDoItem(partitionKey, rowKey);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string partitionKey, string rowKey)
        {
            return View(await _service.GetDoToItem(partitionKey, rowKey));
        }
    }
    ```
    
7. Add Items page link for navigation.
    * Navigate to **Project** -> **Views** -> **Shared** -> **_Layout.cshtml**
    * Find below peice of code in _Layout.cshtml
       ```cs
       <li class="nav-item">
          <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Index">Home</a>
       </li>
       ```
    * Replace above peice of code with below code
      ```cs
      <li class="nav-item">
       <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Index">Home</a>
      </li>
      <li class="nav-item">
          <a class="nav-link text-dark" asp-area="" asp-controller="Item" asp-action="Index">Items</a>
      </li>
      ```
    
## Exercise 4 - Connect to Azure Storage Account

### Steps
First, we'll add a class that contains the logic to connect to and use Azure Storage Account. We'll encapsulate this logic into a class called `AzureTableStorage` and an interface called `IAzureTableStorage`. This service does the CRUD operations. It also does read feed operations such as listing incomplete items, creating, editing, and deleting the items.

1. Add a new Folder "Services" under your project.
2. Add a new class `IAzureTableStorage.cs` under Services folder.
3. Replace the contents of `IAzureTableStorage.cs` with the following code:
    ```cs
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public interface IAzureTableStorage<T> where T : TableEntity, new()
    {
        Task Delete(string partitionKey, string rowKey);
        Task<T> GetItem(string partitionKey, string rowKey);
        Task<List<T>> GetList();
        Task Insert(T item);
        Task Update(T item);
    }
    ```
    
4. Add a new class `AzureTableStorage.cs` under Services Folder, and replace it's contents with the following code:
    ```cs
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class AzureTableStorage<T> : IAzureTableStorage<T>
        where T : TableEntity, new()
    {
        public AzureTableStorage(AzureTableSettings settings)
        {
            this.settings = settings;
        }

        public async Task<List<T>> GetList()
        {
            //Table
            CloudTable table = await GetTableAsync();

            //Query
            TableQuery<T> query = new TableQuery<T>();

            List<T> results = new List<T>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<T> queryResults =
                    await table.ExecuteQuerySegmentedAsync(query, continuationToken);

                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);

            } while (continuationToken != null);

            return results;
        }

        public async Task<T> GetItem(string partitionKey, string rowKey)
        {
            //Table
            CloudTable table = await GetTableAsync();

            //Operation
            TableOperation operation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            //Execute
            TableResult result = await table.ExecuteAsync(operation);

            return (T)(dynamic)result.Result;
        }

        public async Task Insert(T item)
        {
            //Table
            CloudTable table = await GetTableAsync();

            //Operation
            TableOperation operation = TableOperation.Insert(item);

            //Execute
            await table.ExecuteAsync(operation);
        }

        public async Task Update(T item)
        {
            //Table
            CloudTable table = await GetTableAsync();

            //Operation
            TableOperation operation = TableOperation.InsertOrReplace(item);

            //Execute
            await table.ExecuteAsync(operation);
        }

        public async Task Delete(string partitionKey, string rowKey)
        {
            //Item
            T item = await GetItem(partitionKey, rowKey);

            //Table
            CloudTable table = await GetTableAsync();

            //Operation
            TableOperation operation = TableOperation.Delete(item);

            //Execute
            await table.ExecuteAsync(operation);
        }

        private readonly AzureTableSettings settings;

        private async Task<CloudTable> GetTableAsync()
        {
            //Account
            CloudStorageAccount storageAccount = new CloudStorageAccount(
                new StorageCredentials(this.settings.StorageAccount, this.settings.StorageKey), true);

            //Client
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //Table
            CloudTable table = tableClient.GetTableReference(this.settings.TableName);
            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
    ```

5. Add a new class `IToDoItemService.cs` under Services Folder and replace it's contents with the following code:
    ```cs
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public interface IToDoItemService
    {
        Task AddToDoItem(ToDoItem item);
        Task DeleteToDoItem(string releaseYear, string title);
        Task<ToDoItem> GetDoToItem(string category, string rowKey);
        Task<List<ToDoItem>> GetToDoList();
        Task UpdateToDoItem(ToDoItem item);
    }
    ```
    
6. Add a new class `ToDoItemService.cs` under Services Folder and replace it's contents with the following code:
    ```cs
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class ToDoItemService : IToDoItemService
    {
        private readonly IAzureTableStorage<ToDoItem> repository;

        public ToDoItemService(IAzureTableStorage<ToDoItem> repository)
        {
            this.repository = repository;
        }

        public async Task<List<ToDoItem>> GetToDoList()
        {
            return await this.repository.GetList();
        }

        public async Task<ToDoItem> GetDoToItem(string category, string rowKey)
        {
            return await this.repository.GetItem(category, rowKey);
        }

        public async Task AddToDoItem(ToDoItem item)
        {
            await this.repository.Insert(item);
        }

        public async Task UpdateToDoItem(ToDoItem item)
        {
            await this.repository.Update(item);
        }

        public async Task DeleteToDoItem(string releaseYear, string title)
        {
            await this.repository.Delete(releaseYear, title);
        }
    }
    ```
    
7. Within the **Startup.cs** file, add the following line to the `ConfigureServices` method:
   ```cs
   services.AddScoped<IAzureTableStorage<ToDoItem>>(factory =>
   {
       return new AzureTableStorage<ToDoItem>(
           new AzureTableSettings(
               storageAccount: Configuration["Table_StorageAccount"],
               storageKey: Configuration["Table_StorageKey"],
               tableName: Configuration["Table_TableName"]));
   });
   services.AddScoped<IToDoItemService, ToDoItemService>();
   ```

8. Define the configuration in Secret file.
    * Right click on project and click on **Manage User Secrets** and add below settings:
    ```json
      {
        "Table_StorageAccount": "<storage account name>",
        "Table_StorageKey": "UXkeWA0h5afXSpyFvud2YdJ9EPR1NCFHFNu7Pcuq1SjHzdf3H7B4D/Tm+2hgpJeFTGnIGuK0GCpGMid8j1TpIQ==",
        "Table_TableName": "storagedemotable"
      }
    ```
    * Replace the account URI and key from the values in the portal. In the Azure portal, in your Azure storage accounts's blade:
      * Navigate to **Access keys**
      * Copy the storage account name replace the value in 'secret.json' for **Table_StorageAccount**
      * Use value of Key from Key1 and replace the value in 'secret.json' for **Table_StorageKey** 

## Exercise 5 - Run and Test your application

### Steps
1. Select F5 in Visual Studio to build the application in debug mode. It should build the application and launch a browser.
2. Click on Items menu tab, it will open a page with empty grid.
3. Select the Create New link and add values to the Name and Description fields. Leave the Completed check box unselected.
4. Select Edit next to an Item on the list. The app opens the Edit view where you can update any property of your object, including the Completed flag.
5. Verify the data in the Azure storage portal using Data Explorer.

## Exercise 6 - Extend your application (Optional)

### Steps
1. Hide the completed items from the grid.
2. Enable your application to accept the Quantity of items.

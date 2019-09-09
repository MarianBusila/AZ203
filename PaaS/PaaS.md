# Develop Azure Platform as a Service compute solutions (20-25%)

## Create Azure App Service Web Apps

* create an Azure App Service Web App [NetCore](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-get-started-dotnet), [NetCore with SqlDB](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-dotnetcore-sqldb)
    - Create production SQl Database
    ```sh
    az group create --name myResourceGroup --location eastus
    
    az sql server create --name mysqlserver201908 --resource-group myResourceGroup --location eastus --admin-user <db_username> --admin-password <db_password>

    az sql server firewall-rule create --resource-group myResourceGroup --server mysqlserver201908 --name AllowAllIps --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

    az sql db create --resource-group myResourceGroup --server mysqlserver201908 --name coreDB --service-objective S0    
    ```

    - Create App Service plan & Web app
    ```sh
    az appservice plan create --name myAppServicePlan --resource-group myResourceGroup --sku FREE

    az webapp create --resource-group myResourceGroup --plan myAppServicePlan --name todoWebApp201908
    ```

    - Configure connection string & env var
    ```sh
    az webapp config connection-string set --resource-group myResourceGroup --name todoWebApp201908 --settings MyDbConnection="Server=tcp:mysqlserver201908.database.windows.net,1433;Database=coreDB;UserID=<db_username>;Password=<db_password>;Encrypt=true;Connection Timeout=30;" --connection-string-type SQLServer

    az webapp config appsettings set --name todoWebApp201908 --resource-group myResourceGroup --settings ASPNETCORE_ENVIRONMENT="Production"
    ```

    - Publish ToDoList web app

    - Stream diagnostics logs
    ```sh
    az webapp log config --name todoWebApp201908 --resource-group myResourceGroup --application-logging true --level information

    az webapp log tail --name todoWebApp201908 --resource-group myResourceGroup
    ```

* create an Azure App Service background task by using WebJobs [Get started with WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service/webjobs-sdk-get-started)
    - web jobs are build as console applications and deployed in an App Service
    - web jobs can have triggers as azure functions (timer, queue ,etc). They can also be run manually. They can also be long running processes (continuous)
    - you can publish multiple web jobs to a single web app.

* enable diagnostics logging [Azure App Service diagnostics overview](https://docs.microsoft.com/en-us/azure/app-service/overview-diagnostics)

## Create Azure App Service mobile apps (TODO)

* add push notifications for mobile apps
* enable offline sync for mobile app
* implement a remote instrumentation strategy for mobile devices

## Create Azure App Service API apps

* create an Azure App Service API app [Tutorial: Host a RESTful API with CORS in Azure App Service](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-rest-api)
    - open TodoApi solution
    - create web app in app service plan and deploy the TodoApi
    ```sh
    az group create --name myResourceGroup --location eastus
    az appservice plan create --name myAppServicePlan --resource-group myResourceGroup --sku FREE
    az webapp create --resource-group myResourceGroup --plan myAppServicePlan --name todoApi201908
    ``` 
    - check that swagger UI can be accessed at http://todoapi201908.azurewebsites.net/swagger/index.html
    - in the site.js file modify the uri to "const uri = "https://todoapi201908.azurewebsites.net/api/todo";"
    - the local UI will not be able to access the service from Azure due to CORS policy.
    - enable CORS
    ```sh
    az resource update --name web --resource-group myResourceGroup --namespace Microsoft.Web --resource-type config --parent sites/todoapi201908 --set properties.cors.allowedOrigins="['https://localhost:44381']" --api-version 2015-06-01
    ```
    - you can use your own CORS utilities instead of App Service CORS for more flexibility. For example, you may want to specify different allowed origins for different routes or methods. Since App Service CORS lets you specify one set of accepted origins for all API routes and methods, you would want to use your own CORS code


* create documentation for the API by using open source and other tools
    - documentation can be generated with NSwag (ReDeoc UI) and Swashbuckle
    - Swashbuckle
    ```cs
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the Swagger generator, defining 1 or more Swagger documents
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger();

        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        });
    }
    ```
    - Swagger doc can be accessed at: http://localhost:port/swagger/v1/swagger.json
    - Swagger UI can be accessed at: http://localhost:port/swagger

## Implement Azure functions

* implement input and output bindings for a function [Azure Functions triggers and bindings concepts](https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings)
    - function.json
    ```json
    {
    "bindings": [
        {
        "type": "queueTrigger",
        "direction": "in",
        "name": "order",
        "queueName": "myqueue-items",
        "connection": "MY_STORAGE_ACCT_APP_SETTING"
        },
        {
        "type": "table",
        "direction": "out",
        "name": "$return",
        "tableName": "outTable",
        "connection": "MY_TABLE_STORAGE_ACCT_APP_SETTING"
        }
    ]
    }
    ```
    - binding in function.json
    ```json
    {
        "dataType": "binary", // stream, string, binary
        "type": "httpTrigger", // queueTrigger, etc
        "name": "req",
        "direction": "in" // in, out
    }
    ```
    - binding expressions and patterns
    ```cs
    [FunctionName("ResizeImage")]
    public static void Run(
        [BlobTrigger("sample-images/{filename}")] Stream image,
        [Blob("sample-images-sm/{filename}", FileAccess.Write)] Stream imageSmall,
        string filename,
        ILogger log)
    {
        log.LogInformation($"Blob trigger processing: {filename}");
        // ...
    }
    ```
* implement function triggers by using data operations, timers, and webhooks
    - [Cosmos DB](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-cosmos-db-triggered-function), [Cosmos DB Trigger and Bindings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2)
        - CosmosDB trigger works only for inserts and updates, not deletions
        ```cs
        [FunctionName("CosmosTrigger")]
        public static void Run([CosmosDBTrigger(
            databaseName: "ToDoItems",
            collectionName: "Items",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> documents,
            ILogger log)
        {
            if (documents != null && documents.Count > 0)
            {
                log.LogInformation($"Documents modified: {documents.Count}");
                log.LogInformation($"First document Id: {documents[0].Id}");
            }
        }
        ```
        - input binding examples
        ```cs
        [FunctionName("DocByIdFromRouteData")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                Route = "todoitems/{id}")]HttpRequest req,
            [CosmosDB(
                databaseName: "ToDoItems",
                collectionName: "Items",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}")] ToDoItem toDoItem,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            if (toDoItem == null)
            {
                log.LogInformation($"ToDo item not found");
            }
            else
            {
                log.LogInformation($"Found ToDo item, Description={toDoItem.Description}");
            }
            return new OkResult();
        }

        [FunctionName("DocByIdFromRouteDataUsingSqlQuery")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                Route = "todoitems2/{id}")]HttpRequest req,
            [CosmosDB("ToDoItems", "Items",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "select * from ToDoItems r where r.id = {id}")]
                IEnumerable<ToDoItem> toDoItems,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            foreach (ToDoItem toDoItem in toDoItems)
            {
                log.LogInformation(toDoItem.Description);
            }
            return new OkResult();
        }

        [FunctionName("DocsByUsingDocumentClient")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                Route = null)]HttpRequest req,
            [CosmosDB(
                databaseName: "ToDoItems",
                collectionName: "Items",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var searchterm = req.Query["searchterm"];
            if (string.IsNullOrWhiteSpace(searchterm))
            {
                return (ActionResult)new NotFoundResult();
            }

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("ToDoItems", "Items");

            log.LogInformation($"Searching for: {searchterm}");

            IDocumentQuery<ToDoItem> query = client.CreateDocumentQuery<ToDoItem>(collectionUri)
                .Where(p => p.Description.Contains(searchterm))
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                foreach (ToDoItem result in await query.ExecuteNextAsync())
                {
                    log.LogInformation(result.Description);
                }
            }
            return new OkResult();
        }
        ```

        - output binding examples
        ```cs
        [FunctionName("WriteDocsIAsyncCollector")]
        public static async Task Run(
            [QueueTrigger("todoqueueforwritemulti")] ToDoItem[] toDoItemsIn,
            [CosmosDB(
                databaseName: "ToDoItems",
                collectionName: "Items",
                ConnectionStringSetting = "CosmosDBConnection")]
                IAsyncCollector<ToDoItem> toDoItemsOut,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed {toDoItemsIn?.Length} items");

            foreach (ToDoItem toDoItem in toDoItemsIn)
            {
                log.LogInformation($"Description={toDoItem.Description}");
                await toDoItemsOut.AddAsync(toDoItem);
            }
        }
        ```
    - [Blob Storage](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-storage-blob-triggered-function), [Blob Storage Trigger and Bindings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob)
        - use EventGrid trigger instead of Blob storage trigger for blob storage accounts, high scale, minimize latency. Or use a queue trigger
        ```cs
        [FunctionName("ResizeImage")]
        public static void Run(
            [BlobTrigger("sample-images/{name}", Connection = "StorageConnectionAppSetting")] Stream image,
            [Blob("sample-images-md/{name}", FileAccess.Write)] Stream imageSmall)
        {
            ....
        }
        ```
        - blob receipts (stored in in container *azure-webjobs-hosts*) ensure that a blob trigger function is not called more than once for the same new or updated blob        
        - poison blobs (stored in a queue *webjobs-blobtrigger-poison*) 
    - [Queue Storage](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-storage-queue-triggered-function), [Queue Storage Trigger and Bindings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue)
        - trigger example
        ```cs
        [FunctionName("QueueTrigger")]
        public static void QueueTrigger(
            [QueueTrigger("myqueue-items")] string myQueueItem, 
            ILogger log)
        {
            log.LogInformation($"C# function processed: {myQueueItem}");
        }
        ```
        - output binding example
        ```cs
        [FunctionName("QueueOutput")]
        [return: Queue("myqueue-items")]
        public static string QueueOutput([HttpTrigger] dynamic input,  ILogger log)
        {
            log.LogInformation($"C# function processed: {input.Text}");
            return input.Text;
        }
        ```
    - [Timer](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-scheduled-function), [Timer Trigger](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer)
        - trigger example
        ```cs
        [FunctionName("TimerTriggerCSharp")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            if (myTimer.IsPastDue)
            {
                log.LogInformation("Timer is running late!");
            }
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
        ```
        - *runOnStartup* should rarely if ever be set to true, especially in production
    - [Webhook](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook#trigger)
        - customize HTTP endpoint
        ```json
        {
        "bindings": [
        {
            "type": "httpTrigger",
            "name": "req",
            "direction": "in",
            "methods": [ "get" ],
            "route": "products/{category:alpha}/{id:int?}"
        },
        {
            "type": "http",
            "name": "res",
            "direction": "out"
        }
        ]    
        }
        ```
        ```cs
        public static Task<IActionResult> Run(HttpRequest req, string category, int? id, ILogger log) {  }
        ```

        - working with client identities
        ```cs
        public static IActionResult Run(HttpRequest req, ILogger log)
        {
            ClaimsPrincipal identities = req.HttpContext.User;
            // ...
            return new OkObjectResult();
        }
        ```
        - authorization keys: host keys and function keys. Select a function and go to **Manage** to manage the keys. To call a function with a key: *https://<APP_NAME>.azurewebsites.net/api/<FUNCTION_NAME>?code=<API_KEY>*. These keys should be used just in dev. To secure an endpoint in production there are several options:
            - turning on **Authentication/Authorization** for the function and select a provider like AAD, Facebook, etc.
            - use Azure API Management (APIM). The function can be configured to receive requests only from the IP address of the APIM instance.
        
* implement Azure Durable Functions
* create Azure Function apps by using Visual Studio
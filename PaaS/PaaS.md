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

* implement input and output bindings for a function
* implement function triggers by using data operations, timers, and webhooks
* implement Azure Durable Functions
* create Azure Function apps by using Visual Studio
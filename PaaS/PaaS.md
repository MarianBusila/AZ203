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

* create an Azure App Service background task by using WebJobs
* enable diagnostics logging

## Create Azure App Service mobile apps

* add push notifications for mobile apps
* enable offline sync for mobile app
* implement a remote instrumentation strategy for mobile devices

## Create Azure App Service API apps

* create an Azure App Service API app
* create documentation for the API by using open source and other tools

## Implement Azure functions

* implement input and output bindings for a function
* implement function triggers by using data operations, timers, and webhooks
* implement Azure Durable Functions
* create Azure Function apps by using Visual Studio
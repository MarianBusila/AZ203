# Develop for Azure storage (15-20%)

## Develop solutions that use storage tables

* design and implement policies for tables
    - Microsoft recomends CosmosDB Table API over the original Azure table storage because it offers throughput-optimized tables, global distribution, and automatic secondary indexes
    - Azure Table storage stores large amounts of structured data
    - three options for authorizing access to data objects in Azure Storage
        - Using Azure AD to authorize access to containers and queues
        - Using your storage account keys to authorize access via Shared Key
        - Using Shared Access Signatures to grant controlled permissions to specific data objects for a specific amount of time
    - a Shared Access Signature is a set of query parameters appended to the URL pointing at the resource, that provides information about the access allowed and the length of time for which the access is permitted

    ```
    http://mystorage.blob.core.windows.net/mycontainer/myblob.txt (URL to the blob)
    ?sv=2015-04-05 (storage service version)
    &st=2015-12-10T22%3A18%3A26Z (start time, in UTC time and URL encoded)
    &se=2015-12-10T22%3A23%3A26Z (end time, in UTC time and URL encoded)
    &sr=b (resource is a blob)
    &sp=r (read access)
    &sip=168.1.5.60-168.1.5.70 (requests can only come from this range of IP addresses)
    &spr=https (only allow HTTPS requests)
    &sig=Z%2FRHIX5Xcg0Mq2rqI3OlWTjEg2tYkboXr1P9ZUXDtkk%3D (signature used for the authentication of the SAS)
    ```
    - if you have a logical set of parameters that are similar each time, using a Stored Access Policy is a better idea. This SAP can server as a basis for the SAS URI you create

* query table storage by using code [.NET](https://docs.microsoft.com/en-ca/azure/cosmos-db/tutorial-develop-table-dotnet?toc=%2Fen-us%2Fazure%2Fstorage%2Ftables%2FTOC.json&bc=%2Fen-us%2Fazure%2Fbread%2Ftoc.json)
    ```cs
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.CosmosDB.Table;

    var connectionString = "DefaultEndpointsProtocol=https;AccountName=table201909;AccountKey=********;TableEndpoint=https://table201909.table.cosmos.azure.com:443/;";
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
    CloudTable table = tableClient.GetTableReference(tableName);

    // insert or merge
    TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
    TableResult result = await table.ExecuteAsync(insertOrMergeOperation);

    // retrieve
    TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(partitionKey, rowKey);
    TableResult result = await table.ExecuteAsync(retrieveOperation);

    // delete
    TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
    TableResult result = await table.ExecuteAsync(deleteOperation);

    // query
    TableQuery<CustomerEntity> query = new TableQuery<CustomerEntity>()
    .Where(
        TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Smith"),
            TableOperators.And,
            TableQuery.GenerateFilterCondition("Email", QueryComparisons.Equal,"Ben@contoso.com")
    ));

    await table.ExecuteQuerySegmentedAsync<CustomerEntity>(query, null);
    ```
* implement partitioning schemes
    - in general there are 3 strategies for partitioning data
        - **Horizontal** (sharding). Each partition is known as a shard and holds a specific subset of the data, such as all the orders for a specific set of customers.
        - **Vertical**. Each partition holds a subset of the fields for items in the data store. The fields are divided according to their pattern of use. For example, frequently accessed fields might be placed in one vertical partition and less frequently accessed fields in another.
        - **Functional partitioning**. In this strategy, data is aggregated according to how it is used by each bounded context in the system. For example, an e-commerce system might store invoice data in one partition and product inventory data in another.

## Develop solutions that use Cosmos DB storage
* create, read, update, and delete data by using appropriate APIs [.NET](https://docs.microsoft.com/en-us/azure/cosmos-db/sql-api-get-started)
    - *Request Units* per second is a rate-based currency. It abstracts the system resources such as CPU, IOPS, and memory that are required to perform the database operations supported by Azure Cosmos DB
    ```cs
    this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
    this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
    this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/LastName");

    // insert item to container
    try
    {
        ItemResponse<Family> andersenFamilyResponse = await this.container.ReadItemAsync<Family>(andersenFamily.Id, new PartitionKey(andersenFamily.LastName));
    }
    catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        ItemResponse<Family> andersenFamilyResponse = await this.container.CreateItemAsync<Family>(andersenFamily, new PartitionKey(andersenFamily.LastName));
        
        Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", andersenFamilyResponse.Resource.Id, andersenFamilyResponse.RequestCharge);
    }

    // query
    var sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";    
    QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
    FeedIterator<Family> queryResultSetIterator = this.container.GetItemQueryIterator<Family>(queryDefinition);

    while(queryResultSetIterator.HasMoreResults)
    {
        FeedResponse<Family> currentResultSet = await queryResultSetIterator.ReadNextAsync();
        foreach(Family family in currentResultSet)
        {
            Console.WriteLine("\t Read {0}\n", family);
        }        
    }

    // replace
    wakefieldFamilyResponse = await this.container.ReplaceItemAsync<Family>(itemBody, itemBody.Id, new PartitionKey(itemBody.LastName));

    // delete
    ItemResponse<Family> wakefieldFamilyResponse = await this.container.DeleteItemAsync<Family>(familyId,new PartitionKey(partitionKeyValue));

    // delete database
    DatabaseResponse databaseResourceResponse = await this.database.DeleteAsync();

    ```

* implement partitioning schemes [Partitioning in Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/partitioning-overview), [Data partitioning strategies(by service)](https://docs.microsoft.com/en-us/azure/architecture/best-practices/data-partitioning-strategies#partitioning-cosmos-db)
    - Azure Cosmos DB transparently and automatically manages the placement of logical partitions on physical partitions to efficiently satisfy the scalability and performance needs of the container.
    - Azure Cosmos DB uses hash-based partitioning to spread logical partitions across physical partitions
    - A single logical partition has an upper limit of 10 GB of storage
    - Pick a partition key that doesn't result in "hot spots" within your application, because of throughput (RU/s) allocated
    - Choose a partition key that spreads the workload evenly across all partitions and evenly over time. Your choice of partition key should balance the need for efficient partition queries and transactions against the goal of distributing items across multiple partitions to achieve scalability.
    - Candidates for partition keys might include properties that appear frequently as a filter in your queries. Queries can be efficiently routed by including the partition key in the filter predicate.
    - Internally, one or more logical partitions are mapped to a physical partition that consists of a set of replicas
    - Troughput provisioned for a container is divided evenly among physical partitions. A partition key design that doesn't distribute the throughput requests evenly might create "hot" partitions.
    - Cosmos DB supports programmable items that can all be stored in a collection alongside documents. These include stored procedures, user-defined functions, and triggers (written in JavaScript)
    - Database queries are scoped to the collection level
    - Queries with filters on partition key are more efficient

* set the appropriate consistency level for operations [Choose the right consistency level](https://docs.microsoft.com/en-us/azure/cosmos-db/consistency-levels-choosing)
    - there are tradeoffs between consistency, performance and availability and CosmosDB offers 5 consistency levels
    - *Strong* consistency - see all previous writes. 
    - *Bounded Staleness* - see all "old" writes, aka periodic snapshots, continuous consistency. The reads might lag behind writes by at most "K" versions (i.e., "updates") of an item or by "T" time interval
    - *Session* - see all writes performed by reader
    - *Consistent Prefix* - see initial sequence of writes. The reader gets a snapshot of the data that existed in a given point in time in the past.
    - *Eventual* consistency - see subset if previous writes; eventually see all writes. A write performed by a client (in the primary copy of a data center) will eventually be replicated in a remote data center.       

* generic notes on CosmosDB
    - provisioned throughput can be set at the database level and will be shared between containers, or at container level
    - [Indexing](https://docs.microsoft.com/en-us/azure/cosmos-db/index-overview): 
        - by default, Azure Cosmos DB automatically indexes every property for all items in your container without having to define any schema or configure secondary indexes.. You can customize the indexing behavior by configuring the indexing policy on a container.
        - Every time an item is stored in a container, its content is projected as a JSON document, then converted into a tree representation
        - The default indexing policy for newly created containers indexes every property of every item, enforcing *range* indexes for any string or number, and *spatial* indexes for any GeoJSON object of type Point
        - A custom indexing policy can specify property paths that are explicitly included or excluded from indexing. By optimizing the number of paths that are indexed, you can lower the amount of storage used by your container and improve the latency of write operations
        - Queries that have an ORDER BY clause with two or more properties require a composite index. You can also define a composite index to improve the performance of many equality and range queries. By default, no composite indexes are defined so you should add composite indexes as needed.
    - You can set Time to Live (TTL) on selected items in an Azure Cosmos container or for the entire container to gracefully purge those items from the system.
    - You can specify a unique key constraint on your Azure Cosmos container
    - [Data modeling in Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/modeling-data), [Data modelling and partitioning - a real world example](https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-model-partition-example)
        - in relational databases, data is **normalized** to avoid storing redundant data. In NoSQL, all data is **embedded / denormalized** in a single document.
        - when to embed
            - There are contained relationships between entities.
            - There are one-to-few relationships between entities.
            - There is embedded data that changes infrequently.
            - There is embedded data that will not grow without bound.
            - There is embedded data that is queried frequently together.
        - having relationships between different documents is possible in CosmosDB, but there is a tradeoff: write operation can be improved, but read will be costly (example of person with a portofolio of stocks). Relational database might be better in this case
        - when to reference
            - Representing one-to-many relationships.
            - Representing many-to-many relationships.
            - Related data changes frequently.
            - Referenced data could be unbounded.
        - data duplication is encoureged to improve read operations



## Develop solutions that use a relational database

* provision and configure relational databases [Quickstart: Create a single database in Azure SQL Database](https://docs.microsoft.com/en-ca/azure/sql-database/sql-database-single-database-get-started?tabs=azure-portal)
    - Deployment models for Azure sql databases: managed instance, single or elastic pool
    - Purchasing models:
        - vCore-based purchasing model lets you choose the number of vCores, the amount of memory, and the amount and speed of storage
        - DTU-based purchasing model offers a blend of compute, memory, IO resources in three service tiers to support lightweight to heavyweight database workloads
    - when allocating resources for a single database, you can select compute tier (provisioned, serverless) and compute generation(gen4, gen5) and set the number of vCores and data max size
    ```sh
    #!/bin/bash
    # Set variables
    subscriptionID=<SubscriptionID>
    resourceGroupName=myResourceGroup-$RANDOM
    location=SouthCentralUS
    adminLogin=azureuser
    password="PWD27!"+`openssl rand -base64 18`
    serverName=mysqlserver-$RANDOM
    databaseName=mySampleDatabase
    drLocation=NorthEurope
    drServerName=mysqlsecondary-$RANDOM
    failoverGroupName=failovergrouptutorial-$RANDOM

    # The ip address range that you want to allow to access your DB. 
    # Leaving at 0.0.0.0 will prevent outside-of-azure connections to your DB
    startip=0.0.0.0
    endip=0.0.0.0

    # Connect to Azure
    az login

    # Set the subscription context for the Azure account
    az account set -s $subscriptionID

    # Create a resource group
    echo "Creating resource group..."
    az group create \
    --name $resourceGroupName \
    --location $location \
    --tags Owner[=SQLDB-Samples]

    # Create a logical server in the resource group
    echo "Creating primary logical server..."
    az sql server create \
    --name $serverName \
    --resource-group $resourceGroupName \
    --location $location  \
    --admin-user $adminLogin \
    --admin-password $password

    # Configure a firewall rule for the server
    echo "Configuring firewall..."
    az sql server firewall-rule create \
    --resource-group $resourceGroupName \
    --server $serverName \
    -n AllowYourIp \
    --start-ip-address $startip \
    --end-ip-address $endip

    # Create a gen5 1vCore database in the server 
    echo "Creating a gen5 2 vCore database..."
    az sql db create \
    --resource-group $resourceGroupName \
    --server $serverName \
    --name $databaseName \
    --sample-name AdventureWorksLT \
    --edition GeneralPurpose \
    --family Gen5 \
    --capacity 2    
    ```

* configure elastic pools for Azure SQL Database [Manage elastic pools in Azure SQL Database](https://docs.microsoft.com/en-ca/azure/sql-database/sql-database-elastic-pool-manage)
    - SQL Database elastic pools are a simple, cost-effective solution for managing and scaling multiple databases that have varying and unpredictable usage demands. The databases in an elastic pool are on a single Azure SQL Database server and share a set number of resources at a set price. Elastic pools in Azure SQL Database enable SaaS developers to optimize the price performance for a group of databases within a prescribed budget while delivering performance elasticity for each database
    - You can configure resources for the pool based either on the DTU-based purchasing model or the vCore-based purchasing model
    - Pools are well suited for a large number of databases with specific utilization patterns. For a given database, this pattern is characterized by low average utilization with relatively infrequent utilization spikes.

    | Cmdlet | Description |
    | ----------- | ----------- |
    | az sql elastic-pool create | Creates an elastic pool |
    | az sql elastic-pool list | Returns a list of elastic pools in a server |
    | az sql elastic-pool list-dbs | Returns a list of databases in an elastic pool |
    | az sql elastic-pool list-editions | Also includes available pool DTU settings, storage limits, and per database settings |
    | az sql elastic-pool update | Updates an elastic pool |
    | az sql elastic-pool delete | Deletes the elastic pool |

* create, read, update, and delete data tables by using code
    - use SQL connection string to connect to database
    ```cs
    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

    builder.DataSource = "<your_server.database.windows.net>"; 
    builder.UserID = "<your_username>";            
    builder.Password = "<your_password>";     
    builder.InitialCatalog = "<your_database>";

    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
    {
        Console.WriteLine("\nQuery data example:");
        Console.WriteLine("=========================================\n");
        
        connection.Open();       
        StringBuilder sb = new StringBuilder();
        sb.Append("SELECT TOP 20 pc.Name as CategoryName, p.name as ProductName ");
        sb.Append("FROM [SalesLT].[ProductCategory] pc ");
        sb.Append("JOIN [SalesLT].[Product] p ");
        sb.Append("ON pc.productcategoryid = p.productcategoryid;");
        String sql = sb.ToString();

        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                }
            }
        }
    }
    ```

## Develop solutions that use blob storage

* move items in blob storage between storage accounts or containers
* set and retrieve properties and metadata
* implement blob leasing
* implement data archiving and retention
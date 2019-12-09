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

    // insert batch
    var batchOperation = new TableBatchOperation();
    foreach( var entity in entities)
        batchOperation.Insert(entity);
    await table.ExecuteBatchAsync(batchOperation);

    // retrieve
    TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(partitionKey, rowKey);
    TableResult result = await table.ExecuteAsync(retrieveOperation);
    var ce = (CustomerEntity)result.Result;

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

    var results = await table.ExecuteQuerySegmentedAsync<CustomerEntity>(query, null);
    ```
    - query using REST API
    ```
    https://<mytableendpoint>/People(PartitionKey='Harp',RowKey='Walter') 
    ```

* implement partitioning schemes
    - in general there are 3 strategies for partitioning data
        - **Horizontal** (sharding). Each partition is known as a shard and holds a specific subset of the data, such as all the orders for a specific set of customers.
        - **Vertical**. Each partition holds a subset of the fields for items in the data store. The fields are divided according to their pattern of use. For example, frequently accessed fields might be placed in one vertical partition and less frequently accessed fields in another.
        - **Functional partitioning**. In this strategy, data is aggregated according to how it is used by each bounded context in the system. For example, an e-commerce system might store invoice data in one partition and product inventory data in another.

## Develop solutions that use Cosmos DB storage
* create, read, update, and delete data by using appropriate APIs [.NET](https://docs.microsoft.com/en-us/azure/cosmos-db/sql-api-get-started)
    - *Request Units* per second is a rate-based currency. It abstracts the system resources such as CPU, IOPS, and memory that are required to perform the database operations supported by Azure Cosmos DB
    
    ```sh
    # Create a SQL API Cosmos DB account with session consistency and multi-master enabled
    az cosmosdb create `
    -g $resourceGroupName `
    --name $accountName `
    --kind GlobalDocumentDB `
    --locations "West US=0" "North Central US=1" `
    --default-consistency-level Strong `
    --enable-multiple-write-locations true `
    --enable-automatic-failover true

    # Create a database
    az cosmosdb database create `
    -g $resourceGroupName `
    --name $accountName `
    --db-name $databaseName

    # List account keys
    az cosmosdb list-keys `
    --name $accountName `
    -g $resourceGroupName

    # List account connection strings
    az cosmosdb list-connection-strings `
    --name $accountName `
    -g $resourceGroupName

    az cosmosdb show `
    --name $accountName `
    -g $resourceGroupName `
    --query "documentEndpoint"
    ```
    - Example using CosmosClient which is the latest cosmos client (Microsoft.Azure.Cosmos)
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

    - Example using DocumentClient (Microsoft.Azure.Documents)
    ```cs
    DocumentClient _client = new DocumentClient(new Uri(_endpoint), _key);
    await _client.CreateDatabaseIfNotExistsAsync(
                new Database { Id = _databaseId });

    await _client.CreateDocumentCollectionIfNotExistsAsync(
        UriFactory.CreateDatabaseUri(_databaseId), 
        new DocumentCollection { 
            Id = _collectionId,
            PartitionKey = new PartitionKeyDefinition() { 
                Paths = new Collection<string>(new [] { "/id" })
            }
        });
    
    // insert item
    try
    {
        await _client.ReadDocumentAsync(
                        UriFactory.CreateDocumentUri(
                            databaseId, collectionId, documentId),
            new RequestOptions { 
                PartitionKey = new PartitionKey(documentId) 
            });
        Console.WriteLine(
            $"Family {documentId} already exists in the database");
    }
    catch (DocumentClientException de)
    {
        if (de.StatusCode == HttpStatusCode.NotFound)
        {
            await _client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(
                    databaseId, collectionId), 
                data);
            Console.WriteLine($"Created Family {documentId}");
        }
    }

    // execute query
    var queryOptions = new FeedOptions { 
        MaxItemCount = -1, 
        EnableCrossPartitionQuery = true };

    var sql = "SELECT c.givenName FROM Families f JOIN c IN f.children WHERE f.id = 'WakefieldFamily' ORDER BY f.address.city ASC";
    var sqlQuery = _client.CreateDocumentQuery<JObject>(
            UriFactory.CreateDocumentCollectionUri(
                databaseId, collectionId),
            sql, queryOptions );

    foreach (var result in sqlQuery)
    {
        Console.WriteLine(result);
    }

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
    - CAP Theorem
    ```
    Consistency:  Every read receives the most recent write or an error
    Availability: Every request receives a non-error response - without the guarantee that it contains the most recent write
    Partition tolerance: the system continues to operate despite an arbitrary number of messages being dropped or  delayed by the network between nodes
    ```      
    | Level | Overview | CAP | Uses |
    |-------|-------|-------|-------|
    |Strong Consistency|All writes are read immediatly by anyone. Everyone sees the same thing|C: Highest<br>A: Lowest<br>P: Lowest|Financial, inventory, scheduling|
    |Bounded staleness|Trades off lag for ensuring reads return the most recent write. Lag can be specified in time or number of operations.|C: Consistent to a bound<br>A: Low<br>P: Low|Apps showing status, tracking, scores, tickers|
    |Session|Default consistency in CosmosDB. All reads on the same session(connection) are consistent.|C: Strong for the session<br>A: High<br>P: Moderate|Social apps, fitness apps, shopping cart|
    |Consistent prefix|Bounded staleness without lag / delay. You will read consistent data, but it may be an older version.|C: Low<br>A: High<br>P: Low|Social media (comments, likes), apps with updates like scores|
    |Eventual consistency|Highest availability and performance, but no quarantee that a read withing any specific time, for anyone, sees the latest data. But it will eventually be consistent - no loss due to high availability.|C: Lowest<br>A: Highest<br>P: Highest|Non-ordered updates like reviews and ratings, aggregated status|

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
        - SQL Queries
        ```sql
        SELECT *
        FROM Families f
        WHERE f.id = "AndersenFamily"

        // project family name and city
        SELECT {"Name":f.id, "City":f.address.city} AS Family
        FROM Families f
        WHERE f.address.city = f.address.state

        SELECT c.givenName
        FROM Families f
        JOIN c IN f.children
        WHERE f.id = 'WakefieldFamily'
        ORDER BY f.address.city ASC
        ```



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
    - ExecuteScalar() only returns the value from the first column of the first row of your query.
    - ExecuteReader() returns an object that can iterate over the entire result set.
    - ExecuteNonQuery() does not return data at all: only the number of rows affected by an insert, update, or delete.
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
    - CommandBehaviour enum ecample: SqlDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
        - SequenctialAcccess - handle rows that contain columns with large binary values
        - SingleResult - The query returns a single result set.
        - SingleRow - The query is expected to return a single row of the first result set

## Develop solutions that use blob storage
* overview [Advanced C#](https://github.com/Azure-Samples/storage-blob-dotnet-getting-started/blob/master/BlobStorage/Advanced.cs)
    - blob storage resources: *storage account*, *container*, *blob* (block blobs, append blobs, page blobs)
    - hierarchical namespace are supported to have a file system like structure using blob storage. Delimiter **/** is used in blob name to model the hierarchy.
    - access tiers: hot (accessed frequently), cool (infrequently accessed and stored for at least 30 days), archive (rarely accessed and stored for at least 180 days with flexible latency requirements (on the order of hours))
    - data redundancy:(Locally redundant storage ),Zone-redundant storage (ZRS), Geo-redundant storage (GRS), Read-access geo-redundant storage (RA-GRS), Geo-zone-redundant storage (GZRS), Read-access geo-zone-redundant storage (RA-GZRS)
    - leases can be aquired on blobs to make sure they are not modified / deleted by other clients
    - example access snapshot: http://storagesample.core.blob.windows.net/mydrives/myvhd?snapshot=2011-03-09T01:42:34.9360000Z
    - blob quickstart in .NET
    ```cs
    // Retrieve storage account information from connection string.
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

    // Create a container called 'quickstartblobs'
    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("quickstartblobs");
    await cloudBlobContainer.CreateIfNotExistsAsync();

    // Set the permissions so the blobs are public.
    BlobContainerPermissions permissions = new BlobContainerPermissions
    {
        PublicAccess = BlobContainerPublicAccessType.Blob
    };
    await cloudBlobContainer.SetPermissionsAsync(permissions);

    // Upload file
    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(localFileName);
    await cloudBlockBlob.UploadFromFileAsync(sourceFile);

    // List the blobs in the container.
    BlobContinuationToken blobContinuationToken = null;
    do
    {
        var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
        // Get the value of the continuation token returned by the listing call.
        blobContinuationToken = results.ContinuationToken;
        foreach (IListBlobItem item in results.Results)
        {
            Console.WriteLine(item.Uri);
        }
    } while (blobContinuationToken != null); // Loop while the continuation token is not null.

    // Download the blob to a local file
    await cloudBlockBlob.DownloadToFileAsync(destinationFile, FileMode.Create);

    // Delete container
     await cloudBlobContainer.DeleteIfExistsAsync();
    ```
    - advanced options
    ```cs
    // Get current service property settings.
    ServiceProperties serviceProperties = await blobClient.GetServicePropertiesAsync();

    // Enable analytics logging and set retention policy to 14 days. 
    serviceProperties.Logging.LoggingOperations = LoggingOperations.All;
    serviceProperties.Logging.RetentionDays = 14;

    // Acquire the lease. A lease can also be aquired on a blob
    var leaseIdGuid = Guid.NewGuid().ToString();
    leaseId = await container.AcquireLeaseAsync(leaseDuration, leaseIdGuid);

    // delete container will fail if a container is leased, unless the leaseId is provided
    // container.Properties.LeaseState, container.Properties.LeaseDuration, container.Properties.LeaseStatus

    // Break the lease. Passing null indicates that the break interval will be the remainder of the current lease.
    await container.BreakLeaseAsync(null);

    await container.ReleaseLeaseAsync(new AccessCondition() { LeaseId = leaseIdGuid});

    // copy blob
    copyId = await destBlob.StartCopyAsync(sourceBlob);

    //-------------------------------------------------------------------------
    // Creates a shared access policy on the container.
    private static async Task CreateSharedAccessPolicyAsync(CloudBlobContainer container, string policyName)
    {
        // Create a new shared access policy and define its constraints.
        // The access policy provides create, write, read, list, and delete permissions.
        SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
        {
            // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
            // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
            SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
            Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List |
                SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Delete
        };

        // Get the container's existing permissions.
        BlobContainerPermissions permissions = await container.GetPermissionsAsync();

        // Add the new policy to the container's permissions, and set the container's permissions.
        permissions.SharedAccessPolicies.Add(policyName, sharedPolicy);
        await container.SetPermissionsAsync(permissions);
    }

    // Returns a URI containing a SAS for the blob container.
    private static string GetContainerSasUri(CloudBlobContainer container, string storedPolicyName = null)
    {
        string sasContainerToken;

        // If no stored policy is specified, create a new access policy and define its constraints.
        if (storedPolicyName == null)
        {
            // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
            // to construct a shared access policy that is saved to the container's shared access policies. 
            SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List
            };

            // Generate the shared access signature on the container, setting the constraints directly on the signature.
            sasContainerToken = container.GetSharedAccessSignature(adHocPolicy, null);

            Console.WriteLine("SAS for blob container (ad hoc): {0}", sasContainerToken);
            Console.WriteLine();
        }
        else
        {
            // Generate the shared access signature on the container. In this case, all of the constraints for the
            // shared access signature are specified on the stored access policy, which is provided by name.
            // It is also possible to specify some constraints on an ad-hoc SAS and others on the stored access policy.
            sasContainerToken = container.GetSharedAccessSignature(null, storedPolicyName);

            Console.WriteLine("SAS for blob container (stored access policy): {0}", sasContainerToken);
            Console.WriteLine();
        }

        // Return the URI string for the container, including the SAS token.
        return container.Uri + sasContainerToken;
    }

    // Returns a URI containing a SAS for the blob.
    private static string GetBlobSasUri(CloudBlobContainer container, string blobName, string policyName = null)
    {
        string sasBlobToken;

        // Get a reference to a blob within the container.
        // Note that the blob may not exist yet, but a SAS can still be created for it.
        CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

        if (policyName == null)
        {
            // Create a new access policy and define its constraints.
            // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
            // to construct a shared access policy that is saved to the container's shared access policies. 
            SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
            };

            // Generate the shared access signature on the blob, setting the constraints directly on the signature.
            sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);

            Console.WriteLine("SAS for blob (ad hoc): {0}", sasBlobToken);
            Console.WriteLine();
        }
        else
        {
            // Generate the shared access signature on the blob. In this case, all of the constraints for the
            // shared access signature are specified on the container's stored access policy.
            sasBlobToken = blob.GetSharedAccessSignature(null, policyName);

            Console.WriteLine("SAS for blob (stored access policy): {0}", sasBlobToken);
            Console.WriteLine();
        }

        // Return the URI string for the container, including the SAS token.
        return blob.Uri + sasBlobToken;
    }

    ```

* move items in blob storage between storage accounts or containers [AzCopy](https://docs.microsoft.com/en-us/azure/storage/scripts/storage-common-transfer-between-storage-accounts?toc=%2fpowershell%2fmodule%2ftoc.json)
    - *New-AzStorageContext* creates and check storage context with storage account name and key
    - *Get-AzStorageContainer* lists all the containers in a storage account
    - *AzCopy* command to copy each container from the source storage account to the destination storage account
    ```ps
    $azCopyCmd = [string]::Format("""{0}"" /source:{1} /dest:{2} /sourcekey:""{3}"" /destkey:""{4}"" /snapshot /y /s /synccopy",$AzCopyPath, $container.CloudBlobContainer.Uri.AbsoluteUri, $destContainer.Uri.AbsoluteUri, $srcStorageAccountKey, $DestStorageAccountKey)    
    ```

* set and retrieve properties and metadata
    - Blob containers support **system properties** and **user-defined metadata** (name value pairs), in addition to the data they contain
    - *FetchAttributesAsync* must be called first, before reading them
    - Metadata names must be valid HTTP header names and valid C# identifiers
    - Retrieve container properties
    ```cs
    async Task ReadContainerPropertiesAsync(CloudBlobContainer container)
    {
        // Fetch some container properties and write out their values.
        await container.FetchAttributesAsync();
        Console.WriteLine("Properties for container {0}", container.StorageUri.PrimaryUri);
        Console.WriteLine("Public access level: {0}", container.Properties.PublicAccess);
        Console.WriteLine("Last modified time in UTC: {0}", container.Properties.LastModified);
    }
    ```
    - Set and retrieve metadata
    ```cs
    async Task AddContainerMetadataAsync(CloudBlobContainer container)
    {
        // Add some metadata to the container.
        container.Metadata.Add("docType", "textDocuments");
        container.Metadata["category"] = "guidance";

        // Set the container's metadata.
        await container.SetMetadataAsync();
    }

    async Task ReadContainerMetadataAsync(CloudBlobContainer container)
    {
        // Fetch container attributes in order to populate the container's properties and metadata.
        await container.FetchAttributesAsync();

        // Enumerate the container's metadata.
        Console.WriteLine("Container metadata:");
        foreach (var metadataItem in container.Metadata)
        {
            Console.WriteLine("\tKey: {0}", metadataItem.Key);
            Console.WriteLine("\tValue: {0}", metadataItem.Value);
        }
    }
    ```
    - Metadata headers are named with the header prefix x-ms-meta- and a custom name
    - Property headers use standard HTTP header names (Content-Length, Cache-Control, etc)
    - REST API container / blob
    ```
    // Retrieving Properties and Metadata
    GET/HEAD https://myaccount.blob.core.windows.net/mycontainer?restype=container
    GET/HEAD https://myaccount.blob.core.windows.net/mycontainer/myblob?comp=metadata

    // Setting Metadata Headers
    PUT https://myaccount.blob.core.windows.net/mycontainer?comp=metadata&restype=container
    PUT https://myaccount.blob.core.windows.net/mycontainer/myblob?comp=metadata 
    PUT https://myaccount.blob.core.windows.net/mycontainer/myblob?comp=properties
    ```

* implement blob leasing [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/storage/blob/lease?view=azure-cli-latest), [.NET](https://www.red-gate.com/simple-talk/cloud/platform-as-a-service/azure-blob-storage-part-8-blob-leases/)
    -  Azure Lease Blob is a mechanism which provides an exclusive lock to the blob storage
    - you can also put a lease on a container, this gives you exclusive delete access to the container. An important thing to note is that while you hold a lease on a container, it has no effect on the ability to update, add, or delete the blobs in that container.

    | Command | Description |
    |---------|-------------|
    | az storage blob lease acquire | Requests a new lease. |
    | az storage blob lease break | Breaks the lease, if the blob has an active lease. |
    | az storage blob lease change | Changes the lease ID of an active lease. |
    | az storage blob lease release | Releases the lease. |
    | az storage blob lease renew | Renews the lease. |

    ```cs
    // Acquire lease for 15 seconds
    string lease = blockBlob.AcquireLease(TimeSpan.FromSeconds(15), null);
    Console.WriteLine("Blob lease acquired. Lease = {0}", lease);

    // Update blob using lease. This operation will succeed
    const string helloText = "Blob updated";
    var accessCondition = AccessCondition.GenerateLeaseCondition(lease);
    blockBlob.UploadText(helloText, accessCondition: accessCondition);
    Console.WriteLine("Blob updated using an exclusive lease");

    //Simulate third party update to blob without lease
    try
    {
        // Below operation will fail as no valid lease provided
        Console.WriteLine("Trying to update blob without valid lease");
        blockBlob.UploadText("Update without lease, will fail");
    }
    catch (StorageException ex)
    {
        if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
            Console.WriteLine("Precondition failure as expected. Blob's lease does not match");
        else
            throw;
    }
    ```
* implement data archiving and retention [Access tiers](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-storage-tiers), [Immutable blobs](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-immutable-storage)
    - access tier can be specified at account level and at blob level. *archive* tier can be applied only at the object level
    - *Premium performance block blob storage*: data stored in SSD, ideal for capturing telemetry data, messaging, static web content, etc
    - *Blob rehydration*: changing archive tier to hot or cool in order to access the blob
    - Blob data cannot be read or modified while in the archive tier until rehydrated; only blob metadata read operations are supported while in archive.
    - Immutable storage for Azure Blob storage enables users to store business-critical data objects in a WORM (Write Once, Read Many) state. This state makes the data non-erasable and non-modifiable for a user-specified interval
    - Immutable storage for Azure Blob storage supports two types of WORM or immutable policies: time-based retention and legal holds
     - Time-based retention policy is unlocked when created. It must be locked for complience.
     - Immutable storage can be used with any blob type as it is set at the container level
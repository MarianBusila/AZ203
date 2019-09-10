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

* implement partitioning schemes

## Develop solutions that use Cosmos DB storage

* create, read, update, and delete data by using appropriate APIs
* implement partitioning schemes
* set the appropriate consistency level for operations

## Develop solutions that use a relational database

* provision and configure relational databases
* configure elastic pools for Azure SQL Database
* create, read, update, and delete data tables by using code

## Develop solutions that use blob storage

* move items in blob storage between storage accounts or containers
* set and retrieve properties and metadata
* implement blob leasing
* implement data archiving and retention
# Connect to and consume Azure and third-party services (20-25%)

## Develop an App Service Logic App

* create a Logic App [Quickstart: Create your first automated workflow with Azure Logic Apps - Azure portal](https://docs.microsoft.com/en-us/azure/logic-apps/quickstart-create-first-logic-app-workflow)
    - Azure Logic Apps is a cloud service that helps you schedule, automate, and orchestrate tasks, business processes, and workflows when you need to integrate apps, data, systems, and services across enterprises or organizations.
    - Every logic app workflow starts with a trigger. Each time that the trigger fires, the Logic Apps engine creates a logic app instance that runs the actions in the workflow. These actions can also include data conversions and flow controls, such as conditional statements, switch statements, loops, and branching. 

* create a custom connector for Logic Apps [Custom Connectors](https://docs.microsoft.com/en-ca/connectors/custom-connectors/)
    - A custom connector is a wrapper around a REST API. Once you have an API with authenticated access, you can use OpenAPI or Postman collection to describe the API
    - Each connector offers a set of operations classified as 'Actions' and 'Triggers'
    - [Create a custom connector from an OpenAPI definition](https://docs.microsoft.com/en-ca/connectors/custom-connectors/define-openapi-definition)
    - [Use a webhook as a trigger for Azure Logic Apps ](https://docs.microsoft.com/en-ca/connectors/custom-connectors/create-webhook-trigger#create-webhook-triggers-from-the-ui)

* create a custom template for Logic Apps [Overview: Automate deployment for Azure Logic Apps by using Azure Resource Manager templates] (https://docs.microsoft.com/en-ca/azure/logic-apps/logic-apps-azure-resource-manager-templates-overview)
    - a Resource Manager template structure
    ```json
    {
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    // Template parameters
    "parameters": {
        "<template-parameter-name>": {
            "type": "<parameter-type>",
            "defaultValue": "<parameter-default-value>",
            "metadata": {
                "description": "<parameter-description>"
            }
        }
    },
    "variables": {},
    "functions": [],
    "resources": [
        {
            // Start logic app resource definition
            "properties": {
                <other-logic-app-resource-properties>,
                "definition": {
                "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
                "actions": {<action-definitions>},
                // Workflow definition parameters
                "parameters": {
                    "<workflow-definition-parameter-name>": {
                        "type": "<parameter-type>",
                        "defaultValue": "<parameter-default-value>",
                        "metadata": {
                            "description": "<parameter-description>"
                        }
                    }
                },
                "triggers": {
                    "<trigger-name>": {
                        "type": "<trigger-type>",
                        "inputs": {
                            // Workflow definition parameter reference
                            "<attribute-name>": "@parameters('<workflow-definition-parameter-name')"
                        }
                    }
                },
                <...>
                },
                // Workflow definition parameter value
                "parameters": {
                "<workflow-definition-parameter-name>": "[parameters('<template-parameter-name>')]"
                },
                "accessControl": {}
            },
            <other-logic-app-resource-definition-attributes>
        }
        // End logic app resource definition
    ],
    "outputs": {}
    }
    ```
    - 
## Integrate Azure Search within solutions

* create an Azure Search index [Portal](https://docs.microsoft.com/en-us/azure/search/search-create-index-portal), [.NET](https://docs.microsoft.com/en-ca/azure/search/search-get-started-dotnet), [REST](https://docs.microsoft.com/en-ca/azure/search/search-get-started-powershell)
    - **Azure Search** is a search-as-a-service cloud solution that gives developers APIs and tools for adding a rich search experience over private, heterogeneous content in web, mobile, and enterprise applications. 
    - In Azure Search, an index is a persistent store of documents and other constructs used for filtered and full text search on an Azure Search service. Conceptually, a document is a single unit of searchable data in your index.
    - An *indexer* in Azure Search is a crawler that extracts searchable data and metadata from an external Azure data source and populates an index based on field-to-field mappings between the index and your data source. This approach is sometimes referred to as a 'pull model' because the service pulls data in without you having to write any code that adds data to an index
    - fields in a index have data types and attributes controlling how the filed is used:
        - **Retrievable** means that it shows up in search results list. You can mark individual fields as off limits for search results by clearing this checkbox, for example for fields used only in filter expressions.
        - **Key** is the unique document identifier. It's always a string, and it is required.
        - **Filterable, Sortable, and Facetable** determine whether fields are used in a filter, sort, or faceted navigation structure.
        - **Searchable** means that a field is included in full text search. Strings are searchable. Numeric fields and Boolean fields are often marked as not searchable. 
    ```cs
    public partial class Hotel
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string HotelId { get; set; }

        [IsSearchable, IsSortable]
        public string HotelName { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string Description { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.FrLucene)]
        [JsonProperty("Description_fr")]
        public string DescriptionFr { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Category { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public DateTimeOffset? LastRenovationDate { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public double? Rating { get; set; }
    }

    // create client
    SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));

    // create index
    var definition = new Index()
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

    serviceClient.Indexes.Create(definition);

    // upload document
    ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);
    var actions = new IndexAction<Hotel>[]
            {
        IndexAction.Upload(
            new Hotel()
            {
                HotelId = "1",
                HotelName = "Secret Point Motel"
            }),
            ,
        IndexAction.Upload(
            new Hotel()
            {
                HotelId = "2",
                HotelName = "Twin Dome Motel",
            })
            };
    var batch = IndexBatch.New(actions);
    indexClient.Documents.Index(batch);

    // query
    SearchParameters parameters = new SearchParameters()
        {
            Filter = "Rating gt 4",
            Select = new[] { "HotelName", "Rating" }
        };
    results = indexClient.Documents.Search<Hotel>("wifi", parameters);
    ```
    - REST API
    ```
    // create index
    PUT https://<YOUR-SEARCH-SERVICE>.search.windows.net/indexes/hotels-quickstart?api-version=2019-05-06

    // load documents
    POST https://<YOUR-SEARCH-SERVICE>.search.windows.net/indexes/hotels-quickstart/docs/index?api-version=2019-05-06

    // search
    GET https://<YOUR-SEARCH-SERVICE>.search.windows.net/indexes/hotels-quickstart/docs?api-version=2019-05-06&search=*&$filter=Rating gt 4&$select=HotelName,Rating
    ```
* import searchable data [Quickstart: Create an Azure Search index using the Azure portal](https://docs.microsoft.com/en-us/azure/search/search-get-started-portal)
    - Indexers automate data ingestion for supported Azure data sources and handle JSON serialization. Connect to Azure SQL Database, Azure Cosmos DB, or Azure Blob storage to extract searchable content in primary data stores. Azure Blob indexers can perform document cracking to extract text from major file formats, including Microsoft Office, PDF, and HTML documents.
    
* query the Azure Search index [Query using Search explorer](https://docs.microsoft.com/en-us/azure/search/search-get-started-portal#query-index)
    - The **search** parameter is used to input a keyword search for full text search
    - The **$count=true** parameter returns the total count of all documents returned
    - The **$top=10** returns the highest ranked 10 documents out of the total
    - The **$filter** parameter returns results matching the criteria you provided
    - **facet** returns a navigation structure that you can pass to a UI control. It returns categories and a count
    - Example:
    ```
    search=beach$filter=Rating gt 4&$count=true
    search=*&facet=Category&$top=2
    search=seatle~&queryType=full - fuzzy search for misspelled words
    search=*&$count=true&$filter=geo.distance(Location,geography'POINT(-122.12 47.67)') le 5 - geospatial search
    ```

## Establish API Gateways

* create an APIM instance

* configure authentication for APIs

* define policies for APIs

## Develop event-based solutions

* implement solutions that use Azure Event Grid

* implement solutions that use Azure Notification Hubs

* implement solutions that use Azure Event Hub

## Develop message-based solutions

* implement solutions that use Azure Service Bus

* implement solutions that use Azure Queue Storage queues
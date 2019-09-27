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

* Overview [Overview](https://docs.microsoft.com/en-us/azure/api-management/api-management-key-concepts)
    - API Management (APIM) is a way to create consistent and modern API gateways for existing back-end services
    - The **API gateway** is the endpoint that:

        - Accepts API calls and routes them to your backends.
        - Verifies API keys, JWT tokens, certificates, and other credentials.
        - Enforces usage quotas and rate limits.
        - Transforms your API on the fly without code modifications.
        - Caches backend responses where set up.
        - Logs call metadata for analytics purposes.

    - The **Azure portal** is the administrative interface where you set up your API program. Use it to:

        - Define or import API schema.
        - Package APIs into products.
        - Set up policies like quotas or transformations on the APIs.
        - Get insights from analytics.
        - Manage users.
        
    - The **Developer portal** serves as the main web presence for developers, where they can:

        - Read API documentation.
        - Try out an API via the interactive console.
        - Create an account and subscribe to get API keys.
        - Access analytics on their own usage.

    - **Operations** in API Management are highly configurable, with control over URL mapping, query and path parameters, request and response content, and operation response caching. Rate limit, quotas, and IP restriction policies can also be implemented at the API or individual operation level.
    - **Products** are how APIs are surfaced to developers. Products in API Management have one or more APIs, and are configured with a title, description, and terms of use. Products can be Open or Protected.
    - **Groups** are used to manage the visibility of products to developers: *Administrators, Developers, Guests*
    - When developers subscribe to a product, they are granted the primary and secondary key for the product. This key is used when making calls into the product's APIs
    - **Policies** are a powerful capability of API Management that allow the Azure portal to change the behavior of the API through configuration. Policies are a collection of statements that are executed sequentially on the request or response of an API. Popular statements include format conversion from XML to JSON and call rate limiting to restrict the number of incoming calls from a developer, and many other policies are available.
        
* create an APIM instance [Create a new Azure API Management service instance](https://docs.microsoft.com/en-us/azure/api-management/get-started-create-service-instance)
    - after creating the APIM service, you can add an API using OpenAPI, WSDL, LogicApp, FunctionApp, etc. You can create a Product that can contain one or multiple APIs, you can mock API responses, you can protect your API by adding inbound (set rate limit per subscription) and outbound policies(remove headers from the reponse, replace string in  the body like the url of the backend), you can monitor the API be checking the Metrics and creating Alerts (for example for unauthorized access), you can add revisions and versions
    - 

* configure authentication for APIs [Create subscriptions in Azure API Management](https://docs.microsoft.com/en-us/azure/api-management/api-management-howto-create-subscriptions),
[How to secure APIs using client certificate authentication in API Management](https://docs.microsoft.com/en-us/azure/api-management/api-management-howto-mutual-certificates-for-clients)
    - After you create a subscription, two API keys are provided to access the APIs. One key is primary, and one is secondary. Client applications that need to consume the published APIs must include a valid subscription key in HTTP requests when they make calls to those APIs (Ocp-Apim-Subscription-key header must be set in the request)
    - API Management provides the capability to secure access to APIs (i.e., client to API Management) also using client certificates. You can validate incoming certificate and check certificate properties against desired values using policy expressions.
    ```xml
    Checking a thumbprint against certificates uploaded to API Management
    <choose>
        <when condition="@(context.Request.Certificate == null || !context.Request.Certificate.Verify()  || !context.Deployment.Certificates.Any(c => c.Value.Thumbprint == context.Request.Certificate.Thumbprint))" >
            <return-response>
                <set-status code="403" reason="Invalid client certificate" />
            </return-response>
        </when>
    </choose>
    ```

* define policies for APIs [API Management policy samples]https://docs.microsoft.com/en-us/azure/api-management/policy-samples), [API Management policies](https://docs.microsoft.com/en-us/azure/api-management/api-management-policies),
[How to set or edit Azure API Management policies](https://docs.microsoft.com/en-us/azure/api-management/set-edit-policies)
    - Policies can be configured globally or at the scope of a Product, API, or Operation
    - Policy scopes are evaluated in the following order:
        1. Global scope
        2. Product scope
        3. API scope
        4. Operation scope
    -
## Develop event-based solutions

* implement solutions that use Azure Event Grid

* implement solutions that use Azure Notification Hubs

* implement solutions that use Azure Event Hub

## Develop message-based solutions

* implement solutions that use Azure Service Bus

* implement solutions that use Azure Queue Storage queues
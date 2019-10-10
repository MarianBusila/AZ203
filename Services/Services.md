# Connect to and consume Azure and third-party services (20-25%)

## Develop an App Service Logic App

* create a Logic App [Quickstart: Create your first automated workflow with Azure Logic Apps - Azure portal](https://docs.microsoft.com/en-us/azure/logic-apps/quickstart-create-first-logic-app-workflow)
    - Azure Logic Apps is a cloud service that helps you schedule, automate, and orchestrate tasks, business processes, and workflows when you need to integrate apps, data, systems, and services across enterprises or organizations.
    - Every logic app workflow starts with a trigger. Each time that the trigger fires, the Logic Apps engine creates a logic app instance that runs the actions in the workflow. These actions can also include data conversions and flow controls, such as conditional statements, switch statements, loops, and branching. 
    - Actions that can fail (like put a message to a queue) can be retried by specifying a retry policy: Default, Exponential Interval, Fixed Interval, None.
    ```json
    "<action-name>": {
    "type": "<action-type>", 
    "inputs": {
        "<action-specific-inputs>",
        "retryPolicy": {
            "type": "<retry-policy-type>",
            "interval": "<retry-interval>",
            "count": <retry-attempts>,
            "minimumInterval": "<minimum-interval>",
            "maximumInterval": "<maximun-interval>"
        },
        "<other-action-specific-inputs>"
    },
    "runAfter": {}
    }
    -----------------------------
    "HTTP": {
   "type": "Http",
   "inputs": {
      "method": "GET",
      "uri": "http://myAPIendpoint/api/action",
      "retryPolicy" : {
         "type": "exponential",
         "interval": "PT7S",
         "count": 4,
         "minimumInterval": "PT5S",
         "maximumInterval": "PT1H"
      }
   },
   "runAfter": {}
    }
    ```

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
    - there is an admin key and a search key
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
            Select = new[] { "HotelName", "Rating" },
            OrderBy = new[] { "lastRenovationDate desc" },
            Top = 2
        };
    results = indexClient.Documents.Search<Hotel>("wifi", parameters);
    foreach (SearchResult<Hotel> result in results)
    {
        Console.WriteLine(result.Document);
    }

    // there is also a SearchIndexClient that can be used for searching
    // var indexClientForQuery = new SearchIndexClient( searchServiceName, "hotels", new SearchCredentials(queryApiKey));
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

* define policies for APIs [API Management policy samples](https://docs.microsoft.com/en-us/azure/api-management/policy-samples), [API Management policies](https://docs.microsoft.com/en-us/azure/api-management/api-management-policies),
[How to set or edit Azure API Management policies](https://docs.microsoft.com/en-us/azure/api-management/set-edit-policies),
[API Management access restriction policies](https://docs.microsoft.com/en-us/azure/api-management/api-management-access-restriction-policies)
    - there are 3 types of policies:inbound, outbound and backend
    - Policies can be configured globally or at the scope of a Product, API, or Operation
    - Policy scopes are evaluated in the following order:
        1. Global scope
        2. Product scope
        3. API scope
        4. Operation scope
    - rate-limit / quota policy is per subscription, while rate-limit-by-key / quota-by=key is for a custom key which can be set in *counter-key* attribute (client IP address, or even the subscription)
    - Examples:
    ```xml
    // remove http header
    <set-header name="X-Powered-By" exists-action="delete" />
    
    // find and replace in body
    <find-and-replace from="://conferenceapi.azurewebsites.net" to="://apiphany.azure-api.net/conference"/>

    // throttle to 3 calls per 15 seconds for each subscription Id
    <rate-limit-by-key calls="3" renewal-period="15" counter-key="@(context.Subscription.Id)" />

    //  restrict a single client IP address to only 10 calls every minute, with a total of 1,000,000 calls and 10,000 kilobytes of bandwidth per month
    <rate-limit-by-key  calls="10"
          renewal-period="60"
          counter-key="@(context.Request.IpAddress)" />

    <quota-by-key calls="1000000"
            bandwidth="10000"
            renewal-period="2629800"
            counter-key="@(context.Request.IpAddress)" />
        
    <check-header name="Authorization" failed-check-httpcode="401" failed-check-error-message="Not authorized" ignore-case="false">
        <value>f6dc69a089844cf6b2019bae6d36fac8</value>
    </check-header>

    <rate-limit-by-key  calls="10"
              renewal-period="60"
              increment-condition="@(context.Response.StatusCode == 200)"
              counter-key="@(context.Request.IpAddress)"/>    
    
    // restrict caller IP address
    <ip-filter action="allow">
        <address>13.66.201.169</address>
        <address-range from="13.66.140.128" to="13.66.140.143" />
    </ip-filter>

    // The quota policy enforces a renewable or lifetime call volume and/or bandwidth quota, on a per subscription basis. When the call limit is reached, the caller receives a 403 Forbidden response status code
    <quota calls="10000" bandwidth="40000" renewal-period="3600" />
    ```
## Develop event-based solutions

* implement solutions that use Azure Event Grid [Azure Event Grid documentation](https://docs.microsoft.com/en-us/azure/event-grid/)
    - Azure Event Grid allows you to easily build applications with event-based architectures.
    - There are five concepts in Azure Event Grid that let you get going:
        - **Events** - What happened.
        - **Event sources** - Where the event took place.
        - **Topics** - The endpoint where publishers send events.
        - **Event subscriptions** - The endpoint or built-in mechanism to route events, sometimes to more than one handler. Subscriptions are also used by handlers to intelligently filter incoming events.
        - **Event handlers** - The app or service reacting to the event.
    ```sh
    az eventgrid topic create --name $topicname -l westus2 -g gridResourceGroup

    # subscribe to a custom topic from above
    endpoint=https://$sitename.azurewebsites.net/api/updates
    az eventgrid event-subscription create \
    --source-resource-id "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.EventGrid/topics/$topicname" 
    --name demoViewerSub 
    --endpoint $endpoint

    # subscribe to Azure storage blob created (the topic is managed by Azure, no need to create it)
    az eventgrid event-subscription create `
    --source-resource-id  "/subscriptions/{SubID}/resourceGroups/{RG}/providers/Microsoft.Storage/storageaccounts/s1"
    --name storagesubscription `
    --endpoint-type WebHook `
    --endpoint $viewerendpoint `
    --included-event-types "Microsoft.Storage.BlobCreated" `
    --subject-begins-with "/blobServices/default/containers/testcontainer/"
    ```
    -- sample response for event-subscription creation
    ```json
    {
        "deadLetterDestination": null,
        "destination": {
            "endpointBaseUrl": "https://driveeventgridviewer.azurewebsites.net/api/updates",
            "endpointType": "WebHook",
            "endpointUrl": null
        },
        "filter": {
            "includedEventTypes": [
            "Microsoft.Storage.BlobCreated"
            ],
            "isSubjectCaseSensitive": null,
            "subjectBeginsWith": "/blobServices/default/containers/testcontainer/",
            "subjectEndsWith": ""
        },
        "name": "storagesubscription",
        "resourceGroup": "eventgrid",
        "retryPolicy": {
            "eventTimeToLiveInMinutes": 1440,
            "maxDeliveryAttempts": 30
        },
        "topic": "/subscriptions/e093556c-ddf9-46c5-8337-f08a2ad4cac9/resourceGroups/eventgrid/providers/microsoft.storage/storageaccounts/marian201909sa",
        "type": "Microsoft.EventGrid/eventSubscriptions"
    }
    ```

    ```cs
    string topicEndpoint = "https://<YOUR-TOPIC-NAME>.<REGION-NAME>-1.eventgrid.azure.net/api/events";
    string topicKey = "<YOUR-TOPIC-KEY>";

    string topicHostname = new Uri(topicEndpoint).Host;
    TopicCredentials topicCredentials = new TopicCredentials(topicKey);
    EventGridClient client = new EventGridClient(topicCredentials);

    client.PublishEventsAsync(topicHostname, GetEventsList()).GetAwaiter().GetResult();
    ```

* implement solutions that use Azure Notification Hubs [Azure Notification Hubs Documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/)
    - Azure Notification Hubs provide an easy-to-use and scaled-out push engine that allows you to send notifications to any platform (iOS, Android, Windows, Kindle, Baidu, etc.) from any backend (cloud or on-premises).
    - Push notifications are delivered through platform-specific infrastructures called Platform Notification Systems (PNSes)
    - you create a NotificationHub namespace and add notification hubs inside it
    - to configure a notication hub, you must set the OBS settings for different platforms like Apple Push Notification Service, Google Firebase Cloud Messaging, Microsoft Push Notification Service for Windows Phone, etc

* implement solutions that use Azure Event Hub [Azure Event Hubs documentation](https://docs.microsoft.com/en-us/azure/event-hubs/)
    - Azure Event Hubs is a big data streaming platform and event ingestion service. It can receive and process millions of events per second. Data sent to an event hub can be transformed and stored by using any real-time analytics provider or batching/storage adapters.
    - Event Hubs provides a unified streaming platform with time retention buffer, decoupling event producers from event consumers.
    - Azure Event Hubs Capture enables you to automatically capture the streaming data in Event Hubs in an Azure Blob storage or Azure Data Lake Storage
    - Event Hubs uses a partitioned consumer model, enabling multiple applications to process the stream concurrently and letting you control the speed of processing.
    - Event Hubs contains the following key components:
        - **Event producers**: Any entity that sends data to an event hub. Event publishers can publish events using HTTPS or AMQP 1.0 or Apache Kafka (1.0 and above)
        - **Partitions**: Each consumer only reads a specific subset, or partition, of the message stream.
        - **Consumer groups**: A view (state, position, or offset) of an entire event hub. Consumer groups enable consuming applications to each have a separate view of the event stream. They read the stream independently at their own pace and with their own offsets.
        - **Throughput units**: Pre-purchased units of capacity that control the throughput capacity of Event Hubs.
        - **Event receivers**: Any entity that reads event data from an event hub. All Event Hubs consumers connect via the AMQP 1.0 session. The Event Hubs service delivers events through a session as they become available. All Kafka consumers connect via the Kafka protocol 1.0 and later.
    
    ```cs
    var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
    {
        EntityPath = EventHubName
    };

    EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
    await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
    await eventHubClient.CloseAsync();
    ```
    ```cs
    public class SimpleEventProcessor : IEventProcessor
    {
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");
            }

            return context.CheckpointAsync();
        }
    }

    // in Main
     var eventProcessorHost = new EventProcessorHost(
        EventHubName,
        PartitionReceiver.DefaultConsumerGroupName,
        EventHubConnectionString,
        StorageConnectionString,
        StorageContainerName);

    // Registers the Event Processor Host and starts receiving messages
    await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

    Console.WriteLine("Receiving. Press ENTER to stop worker.");
    Console.ReadLine();

    // Disposes of the Event Processor Host
    await eventProcessorHost.UnregisterEventProcessorAsync();
    ```

## Develop message-based solutions

* implement solutions that use Azure Service Bus [Azure Service Bus Messaging documentation](https://docs.microsoft.com/en-us/azure/service-bus-messaging)
    - Microsoft Azure Service Bus is a fully managed enterprise integration message broker. Service Bus is most commonly used to decouple applications and services from each other, and is a reliable and secure platform for asynchronous data and state transfer. Data is transferred between different applications and services using messages. A message is in binary format, which can contain JSON, XML, or just text.
    - To realize a first-in, first-out (FIFO) guarantee in Service Bus, use sessions. **Message sessions** enable joint and ordered handling of unbounded sequences of related messages.
    - The **auto-forwarding** feature enables you to chain a queue or subscription to another queue or topic that is part of the same namespace
    - Service Bus supports a **dead-letter queue (DLQ)** to hold messages that cannot be delivered to any receiver, or messages that cannot be processed
    - you can submit messages to a queue or topic for **delayed processing**; for example, to schedule a job to become available for processing by a system at a certain time.
    - When a queue or subscription client receives a message that it is willing to process, but for which processing is not currently possible due to special circumstances within the application, the entity has the option to **defer retrieval** of the message to a later point.
    - A **transaction** groups two or more operations together into an execution scope. Service Bus supports grouping operations against a single messaging entity (queue, topic, subscription) within the scope of a transaction.

    - create service bus queue
    ```sh
    # Create a resource group
    resourceGroupName="myResourceGroup"

    az group create --name $resourceGroupName --location eastus

    # Create a Service Bus messaging namespace with a unique name
    namespaceName=myNameSpace$RANDOM
    az servicebus namespace create --resource-group $resourceGroupName --name $namespaceName --location eastus

    # Create a Service Bus queue
    az servicebus queue create --resource-group $resourceGroupName --namespace-name $namespaceName --name BasicQueue

    # Powershell command
    New-AzureRmServiceBusQueue `
    -ResourceGroupName servicebus `
    -NamespaceName laaz203sb `
    -name testqueue `
    -EnablePartitioning $false

    # Get the connection string for the namespace
    connectionString=$(az servicebus namespace authorization-rule keys list --resource-group $resourceGroupName --namespace-name $namespaceName --name RootManageSharedAccessKey --query primaryConnectionString --output tsv)
    ```

    ```cs
    // send messages to queue
    IQueueClient queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
    await queueClient.SendAsync(message);

    await queueClient.CloseAsync();
    ```

    ```cs
    // receive messages from queue
    IQueueClient queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

    var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
    {
        // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
        // Set it according to how many messages the application wants to process in parallel.
        MaxConcurrentCalls = 1,

        // Indicates whether the message pump should automatically complete the messages after returning from user callback.
        // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
        AutoComplete = false
    };

    // Register the function that processes messages.
    queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);

    await queueClient.CloseAsync();

    static async Task ProcessMessagesAsync(Message message, CancellationToken token)
    {
        // Process the message.
        Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

        // Complete the message so that it is not received again.
        // This can be done only if the queue Client is created in ReceiveMode.PeekLock mode (which is the default).
        await queueClient.CompleteAsync(message.SystemProperties.LockToken);

        // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
        // If queueClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
        // to avoid unnecessary exceptions.
    }
    ```

    ```cs
    // send messages to topic
    ITopicClient topicClient = new TopicClient(ServiceBusConnectionString, TopicName);

    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
    await topicClient.SendAsync(message);

    await topicClient.CloseAsync();
    ```

    ```cs
    ISubscriptionClient subscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, SubscriptionName);

    var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
    {
        MaxConcurrentCalls = 1,
        AutoComplete = false
    };

    subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);

    static async Task ProcessMessagesAsync(Message message, CancellationToken token)
    {
        // Process the message.
        Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
    }
    ```

    - the default behaviour for topic is bradcasting. Use topic filters to umplment patterns like partitioning (mutually exclusive), routing (not necessarily exclusive)
    - Message class has properties like To, RelpyTo, ReplyToSessionId, MessageId, CorrelationId and SessionId for implementing patterns like simple request/reply, Multiplexing, etc. For example, set CorrelationId to the MessageId before sending the response to ReplyTo queue. Or set ReplyToSessionId to SessionId before sending the response to ReplyTo queue

* implement solutions that use Azure Queue Storage queues [Get started with Azure Queue storage using .NET](https://docs.microsoft.com/en-us/azure/storage/queues/storage-dotnet-how-to-use-queues)
    ```cs
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
    CloudConfigurationManager.GetSetting("StorageConnectionString"));
    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

    // create queue
    CloudQueue queue = queueClient.GetQueueReference("myqueue");
    queue.CreateIfNotExists();

    // insert message
    CloudQueueMessage message = new CloudQueueMessage("Hello, World");
    queue.AddMessage(message);

    // peek at the message
    CloudQueueMessage peekedMessage = queue.PeekMessage();

    // get the next message
    CloudQueueMessage retrievedMessage = queue.GetMessage();

    //Process the message in less than 30 seconds, and then delete the message
    queue.DeleteMessage(retrievedMessage);

    // Delete the queue.
    queue.Delete();
    ```
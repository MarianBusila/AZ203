# Monitor, Troubleshoot, and Optimize Azure Solutions (10-15%)

## Develop code to support scalability of apps and services

* implement autoscaling rules and patterns (schedule, operational/system metrics, singleton applications) [Autoscaling](https://docs.microsoft.com/en-us/azure/architecture/best-practices/auto-scaling)
    - Vertical scaling, also called scaling up and down, means changing the capacity of a resource. For example, you could move an application to a larger VM size
    - Horizontal scaling, also called scaling out and in, means adding or removing instances of a resource. 
    - Azure Virtual Machines, Service Fabric, Azure AppService, Azure Cloud Services has built in autoscaling
    - Scaling can be performed on a schedule, or based on a runtime metric, such as CPU or memory usage. 
    - custom scaling might be necessary based on business counters like number of orders, etc
    - autoscalling takes time to provision resources. For examepl, for a peak that is short term, other solutions could be used like throttling.
    - patterns: [Competing Consumers](https://docs.microsoft.com/en-us/azure/architecture/patterns/competing-consumers), [Pipes and Filters](https://docs.microsoft.com/en-us/azure/architecture/patterns/pipes-and-filters), [Throttling](https://docs.microsoft.com/en-us/azure/architecture/patterns/throttling)
    - Auto-scaling can be configured using Azure Monitor, which performs the heavy lifting of monitoring and scaling resources based upon rules you define for horizontal scaling (scale in and out).
        - **Sources**: Current resource(app service, VM), Storage Queues, Service Bus Queue, ApplicationInsights
        - **Metrics**: CPU, Memory, Disk Queue Len, Http Queue Len, Data In/Out
    - When defining a rule for auto scaling specify time aggregations and statistic, operator (greater then, less than, ..), threshold and duration. Finally, give the operation: Increase/Decrease count or percentage,  change amount and cool down.

    
* implement code that handles transient faults [Transient fault handling](https://docs.microsoft.com/en-us/azure/architecture/best-practices/transient-faults), [Retry guidance for Azure services](https://docs.microsoft.com/en-us/azure/architecture/best-practices/retry-service-specific)
    - Transient faults include the momentary loss of network connectivity to components and services, the temporary unavailability of a service, or timeouts that arise when a service is busy. These faults are often self-correcting, and if the action is repeated after a suitable delay it is likely to succeed.
    - The application must be able to detect faults when they occur, and determine if these faults are likely to be transient, more long-lasting, or are terminal failures.
    - The application must be able to retry the operation if it determines that the fault is likely to be transient
    - The application must use an appropriate strategy for the retries
    - Use the built-in retry mechanism if available in client or SDK
    - retry intervals: Exponential back-off, Incremental intervals, Regular intervals, Randomization
    - If a process must meet a specific service level agreement (SLA), the overall operation time, including all timeouts and delays, must be within the limits defined in the SLA.
    - avoid implementations that include duplicated layers of retry code
    - Prevent multiple instances of the same client, or multiple instances of different clients, from sending retries at the same times. If this is likely to occur, introduce randomization into the retry intervals
    - For HTTP-based APIs, consider using the FiddlerCore library in your automated tests 
    - consider using a central point for storing all the retry policies in code
    - Log transient faults as Warning entries rather than Error entries so that monitoring systems do not detect them as application errors that may trigger false alerts.
    - To prevent continual retries for operations that continually fail, consider implementing the Circuit Breaker pattern. In this pattern, if the number of failures within a specified time window exceeds the threshold, requests are returned to the caller immediately as errors, without attempting to access the failed resource or service.
    - Consider if retrying the same operation may cause inconsistencies in data (idempotency)
    - some Azure services have retry mechanism in their clients (EventHubs, ServiceBus, Storage, CosmosDB, etc)

## Integrate caching and content delivery within solutions

* store and retrieve data in Azure Redis cache [Quickstart: Use Azure Cache for Redis with a .NET Core app](https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-dotnet-core-quickstart), [ASP.NET Session State Provider for Azure Cache for Redis](https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-aspnet-session-state-provider)
    ```cs
    private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            string cacheConnection = ConfigurationManager.AppSettings["CacheConnection"].ToString();
            return ConnectionMultiplexer.Connect(cacheConnection);
        });

    public static ConnectionMultiplexer Connection
    {
        get
        {
            return lazyConnection.Value;
        }
    }

    IDatabase cache = lazyConnection.Value.GetDatabase();
    cache.StringSet("Message", "Hello! The cache is working from a .NET console app!")
    cache.StringGet("Message");
    ```

    - for session state caching in ASP.NET: install nuget package *Microsoft.Web.RedisSessionStateProvider* which adds to web.config settings to point to your redis cache.

    - for output caching in ASP.NET: install nuget package *Install-Package Microsoft.Web.RedisOutputCacheProvider* which adds to web.config the following section
    ```xml
    <caching>
        <outputCache defaultProvider="MyRedisOutputCache">
            <providers>
            <add name="MyRedisOutputCache" type="Microsoft.Web.Redis.RedisOutputCacheProvider"
                host=""
                accessKey=""
                ssl="true" />
            </providers>
        </outputCache>
    </caching>
    ```

* develop code to implement CDNs in solutions [What is Azure CDN?](https://docs.microsoft.com/en-us/azure/cdn/cdn-overview), [Getting started on managing CDN in .NET](https://github.com/Azure-Samples/cdn-dotnet-manage-cdn)
    - A content delivery network (CDN) is a distributed network of servers that can efficiently deliver web content to users. CDNs store cached content on edge servers in point-of-presence (POP) locations that are close to end users, to minimize latency.
    - you need to create a CDN profile which is a collection of CDN endpoints. An endpoint can have as Origin, for example, a Storage
    - the endpoint name is a subdomain of azureedge.net and is included in the URL for delivering CDN content by default (for example, https://contoso.azureedge.net/photo.png). You can map a custom domain with a CDN endpoint by creating a CNAME DNS Record and associating the custom domain with the CDN endpoint.
    - application code has to be modified to access the CDN endpoint instead of the origin URL
    - you can preload assets in CDN endpoints for imediate availability
    
* invalidate cache content (CDN or Redis) [Purge an Azure CDN endpoint](https://docs.microsoft.com/en-us/azure/cdn/cdn-purge-endpoint)
    - on CDN you can *Purge* the endpoint (Portal or API) or you can do versioning of files
    - Azure CDN edge nodes will cache assets until the asset's time-to-live (TTL) expires

## Instrument solutions to support monitoring and logging

* configure instrumentation in an app or service by using Application Insights
[Application Insights for ASP.NET Core applications](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core), [Application Insights for .NET console applications](https://docs.microsoft.com/en-us/azure/azure-monitor/app/console)
    - DependencyTrackingTelemetryModule currently tracks the following dependencies automatically: Http/Https, SQL, Azure Storage, EventHub Client Sdk, ServiceBus Client Sdk, CosmosDB
    ```cs
    TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
    configuration.InstrumentationKey = instrumentationKey;
    var telemetryClient = new TelemetryClient(configuration);
    telemetryClient.TrackTrace("Hello World!");
    ```
    - the following modules are added automatically
        - RequestTrackingTelemetryModule - Collects RequestTelemetry from incoming web requests.
        - DependencyTrackingTelemetryModule - Collects DependencyTelemetry from outgoing http calls and sql calls.
        - PerformanceCollectorModule - Collects Windows PerformanceCounters.
        - QuickPulseTelemetryModule - Collects telemetry for showing in Live Metrics portal.
        - AppServicesHeartbeatTelemetryModule - Collects heart beats (which are send as custom metrics), about Azure App Service environment where application is hosted.
        - AzureInstanceMetadataTelemetryModule - Collects heart beats (which are send as custom metrics), about Azure VM environment where application is hosted.
        - EventCounterCollectionModule - Collects EventCounters.. This module is a new feature and is available in SDK Version 2.8.0-beta3 and higher.
    - *Telemetry channels* are responsible for buffering telemetry items and sending them to the Application Insights service, where they're stored for querying and analysis. The *Send(ITelemetry item)* method of a telemetry channel is called after all telemetry initializers and telemetry processors are called. So, any items dropped by a telemetry processor won't reach the channel. *InMemoryChannel* and *ServerTelemetryChannel* are built-in.
    - *Sampling* is a feature in Azure Application Insights and it is the recommended way to reduce telemetry traffic and storage, while preserving a statistically correct analysis of application data. Adaptive Sampling retains 1 in n records and discards the rest. For example, it might retain one in five events, a sampling rate of 20%. The sampling divisor n is reported in each record in the property *itemCount*. Other types of sampling: fixed-rate sampling and ingestion sampling (all the telemetry is sent to ApplicationInsights server (no bandwidth saved), but it gets filtered on AppInsights (reduce storage))
    - *Telemetry Initializers* add properties to any telemetry sent from your app, including telemetry from the standard modules. For example, you could add calculated values or version numbers by which to filter the data in the portal. (ITelemetryInitializer)
    - *Telemetry Processors* gives you more direct control over what is included or excluded from the telemetry stream. You can use it in conjunction with Sampling, or separately. All telemetry goes through your processor, and you can choose to drop it from the stream, or add properties. (ITelemetryProcessor)
    - Usage Analytics
        - **Funnels**: Track progression of a user through a series of steps in your app
        - **Impact**:  How do page load times and other properties influnce conversion rates
        - **Retention**: How many users return and how often do they perform particular tasks of achive goals
        - **User Flows**: Show how users navigate between pages and features (repeat actions)
    - Using ApplicationInsightsServiceOptions
    ```cs
    public void ConfigureServices(IServiceCollection services)
    {
        Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions aiOptions
                    = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();
        // Disables adaptive sampling.
        aiOptions.EnableAdaptiveSampling = false;

        // Disables QuickPulse (Live Metrics stream).
        aiOptions.EnableQuickPulseMetricStream = false;
        services.AddApplicationInsightsTelemetry(aiOptions);
    }
    ```
    - Adding Telemetry initializers and processors
    ```cs
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITelemetryInitializer, MyCustomTelemetryInitializer>();
        services.AddApplicationInsightsTelemetryProcessor<MyFirstCustomTelemetryProcessor>();
    }
    ```
    - Configure and remove Telemetry modules
    ```cs
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetry();

        // The following configures DependencyTrackingTelemetryModule.
        // Similarly, any other default modules can be configured.
        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                        {
                            module.EnableW3CHeadersInjection = true;
                        });

        // The following removes all default counters from EventCounterCollectionModule, and adds a single one.
        services.ConfigureTelemetryModule<EventCounterCollectionModule>(
                            (module, o) =>
                            {
                                module.Counters.Clear();
                                module.Counters.Add(new EventCounterCollectionRequest("System.Runtime", "gen-0-size"));
                            }
                        );

        // The following removes PerformanceCollectorModule to disable perf-counter collection.
        // Similarly, any other default modules can be removed.
        var performanceCounterService = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(PerformanceCollectorModule));
        if (performanceCounterService != null)
        {
         services.Remove(performanceCounterService);
        }
    }
    ```
    - Configure a Telemetry channel
    ```cs
    public void ConfigureServices(IServiceCollection services)
    {
        // Use the following to replace the default channel with InMemoryChannel.
        // This can also be applied to ServerTelemetryChannel.
        services.AddSingleton(typeof(ITelemetryChannel), new InMemoryChannel() {MaxTelemetryBufferCapacity = 19898 });

        services.AddApplicationInsightsTelemetry();
    }
    ```


* analyze and troubleshoot solutions by using Azure Monitor [Get started with Log Analytics in Azure Monitor](https://docs.microsoft.com/en-ca/azure/azure-monitor/log-query/get-started-portal)
    - All data collected by Azure Monitor fits into one of two fundamental types, metrics and logs. Metrics are numerical values that describe some aspect of a system at a particular point in time. They are lightweight and capable of supporting near real-time scenarios. Logs contain different kinds of data organized into records with different sets of properties for each type. Telemetry such as events and traces are stored as logs in addition to performance data so that it can all be combined for analysis.
    - You can create and test queries using Log Analytics in the Azure portal and then either directly analyze the data using these tools or save queries for use with visualizations or alert rules.
    - Azure Monitor include features like Application Insights, Azure Monitor for containers and VMs
    - Log Analytics is a web tool used to write and execute Azure Monitor log queries. Open it by selecting Logs in the Azure Monitor menu. It starts with a new blank query.
    - The *schema* is a collection of tables visually grouped under a logical category. Several of the categories are from monitoring solutions. The LogManagement category contains common data such as Windows and Syslog events, performance data, and agent heartbeats.
    ```
    Event 
    | search "error" 
    | where Source == "MSSQLSERVER" 
    | order by TimeGenerated desc

    // available memory(MB) per hour for computers with name starting with Contoso
    Perf 
    | where TimeGenerated > ago(1d) 
    | where Computer startswith "Contoso" 
    | where CounterName == "Available MBytes" 
    | summarize count() by bin(TimeGenerated, 1h), Computer
    | render timechart

    // select and compute columns
    SecurityEvent
    | top 10 by TimeGenerated 
    | project Computer, TimeGenerated, EventDetails=Activity, EventCode=substring(Activity, 0, 4)

    // grouping
    Perf
    | where TimeGenerated > ago(1h)
    | summarize avg(CounterValue) by Computer, CounterName   

    // grouping by time column
    Perf 
    | where TimeGenerated > ago(7d)
    | where Computer == "ContosoAzADDS2" 
    | where CounterName == "Available MBytes" 
    | summarize avg(CounterValue) by bin(TimeGenerated, 1h) 
    ```

    - enable application logging(filesystem) for a webapp
    ```sh
    az webapp log config -n $appName -g $resourceGroup --level information --application-logging true

    # tail the logs
    az webapp log tail -n $appName -g $resourceGroup
    ```

    ```cs
     public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.AddAzureWebAppDiagnostics();
            })
            .UseStartup<Startup>();
    }
    ```

    - to enable application logging to blob, create a storageaccount with a container. There are 2 app settings that are generated when enabling logging to blob (*DIAGNOSTICS_AZUREBLOBCONTAINERSASURL and DIAGNOSTICS_AZUREBLOBRETENTIONINDAYS*)

* implement Application Insights Web Test and Alerts [Monitor the availability of any website](https://docs.microsoft.com/en-us/azure/azure-monitor/app/monitor-web-app-availability), [Add availability test with PowerShell](https://docs.microsoft.com/en-us/azure/azure-monitor/app/powershell#add-an-availability-test)
    - in Azure Application Insights you can configure sending web requests to your application at regular intervals from points around the world.
    - the URL to be tested must be visible from the internet. If your URL is not visible from the public internet, you can choose to selectively open up your firewall to allow only the test transactions through
    - you can create simple URL ping test or multi-step web test

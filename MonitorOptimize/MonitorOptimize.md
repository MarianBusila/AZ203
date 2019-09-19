# Monitor, Troubleshoot, and Optimize Azure Solutions (10-15%)

## Develop code to support scalability of apps and services

* implement autoscaling rules and patterns (schedule, operational/system metrics, singleton applications) [Autoscaling](https://docs.microsoft.com/en-us/azure/architecture/best-practices/auto-scaling)
    - Vertical scaling, also called scaling up and down, means changing the capacity of a resource. For example, you could move an application to a larger VM size
    - Horizontal scaling, also called scaling out and in, means adding or removing instances of a resource. 
    - Azure Virtual Machines, Service Fabric, Azure AppService, Azure Cloud Services has built in autoscaling
    - Scaling can be performed on a schedule, or based on a runtime metric, such as CPU or memory usage. 
    - custome scaling might be necessary based on business counters like number of orders, etc
    - autoscalling takes time to provision resources. For examepl, for a peak that is short term, other solutions could be used like throttling.
    - patterns: [Competing Consumers](https://docs.microsoft.com/en-us/azure/architecture/patterns/competing-consumers), [Pipes and Filters](https://docs.microsoft.com/en-us/azure/architecture/patterns/pipes-and-filters), [Throttling](https://docs.microsoft.com/en-us/azure/architecture/patterns/throttling)
    
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

* store and retrieve data in Azure Redis cache
* develop code to implement CDNs in solutions
* invalidate cache content (CDN or Redis)

## Instrument solutions to support monitoring and logging

* configure instrumentation in an app or service by using Application Insights
* analyze and troubleshoot solutions by using Azure Monitor
* implement Application Insights Web Test and Alerts
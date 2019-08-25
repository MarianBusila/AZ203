using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebJobsSDKSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new HostBuilder();
            builder.UseEnvironment("development");
            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
                b.AddAzureStorage();
            });
            builder.ConfigureLogging((context, b) =>
            {
                b.AddConsole();

                // If the key exists in settings, use it to enable Application Insights.
                string instrumentationKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    b.AddApplicationInsights(o => o.InstrumentationKey = instrumentationKey);
                }
            });
            var host = builder.Build();
            using (host)
            {
                host.Run();
            }
        }
    }
}

using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BatchFFmpeg
{
    class Program
    {
        // Batch account credentials
        private static string BatchAccountName;
        private static string BatchAccountKey;
        private static string BatchAccountUrl;

        // Storage account credentials
        private static string StorageAccountName ;
        private static string StorageAccountKey;

        // Pool and Job constants
        private const string PoolId = "WinFFmpegPool";
        private const int DedicatedNodeCount = 0;
        private const int LowPriorityNodeCount = 5;
        private const string PoolVMSize = "STANDARD_A1_v2";
        private const string JobId = "WinFFmpegJob";

        // Application package Id and version
        const string appPackageId = "ffmpeg";
        const string appPackageVersion = "3.4";

        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.local.json", true, true)
                .Build();

            BatchAccountName = config["BatchAccountName"];
            BatchAccountKey = config["BatchAccountKey"];
            BatchAccountUrl = config["BatchAccountUrl"];
            StorageAccountName = config["StorageAccountName"];
            StorageAccountKey = config["StorageAccountKey"];

            if (String.IsNullOrEmpty(BatchAccountName) ||
                String.IsNullOrEmpty(BatchAccountKey) ||
                String.IsNullOrEmpty(BatchAccountUrl) ||
                String.IsNullOrEmpty(StorageAccountName) ||
                String.IsNullOrEmpty(StorageAccountKey))
            {
                throw new InvalidOperationException("One or more account credential strings have not been populated. Please ensure that your Batch and Storage account credentials have been specified.");
            }

            try
            {
                // Call the asynchronous version of the Main() method. This is done so that we can await various
                // calls to async methods within the "Main" method of this console application.
                MainAsync().Wait();
            }
            catch (AggregateException)
            {
                Console.WriteLine();
                Console.WriteLine("One or more exceptions occurred.");
                Console.WriteLine();
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Sample complete, hit ENTER to exit...");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Provides an asynchronous version of the Main method, allowing for the awaiting of async method calls within.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        private static async Task MainAsync()
        {
            Console.WriteLine("Sample start: {0}", DateTime.Now);
            Console.WriteLine();
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Print out timing info
            timer.Stop();
            Console.WriteLine();
            Console.WriteLine("Sample end: {0}", DateTime.Now);
            Console.WriteLine("Elapsed time: {0}", timer.Elapsed);
        }
    }
}

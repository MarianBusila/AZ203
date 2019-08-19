using Microsoft.Azure.Batch;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Protocol;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;

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
            catch (AggregateException ex)
            {
                Console.WriteLine();
                Console.WriteLine($"One or more exceptions occurred. {ex.Flatten().Message}");
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

            // Construct the Storage account connection string
            string storageConnectionString = String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                                StorageAccountName, StorageAccountKey);

            // Retrieve the storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the blob client, for use in obtaining references to blob storage containers
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Use the blob client to create the containers in blob storage
            const string inputContainerName = "input";
            const string outputContainerName = "output";

            await CreateContainerIfNotExistAsync(blobClient, inputContainerName);
            await CreateContainerIfNotExistAsync(blobClient, outputContainerName);

            // RESOURCE FILE SETUP
            string inputPath = Path.Combine(Environment.CurrentDirectory, "InputFiles");
            List<string> inputFilePaths = new List<string>(Directory.GetFileSystemEntries(inputPath, "*.mp4",
                                         SearchOption.TopDirectoryOnly));

            // Upload data files.
            // Upload the data files using UploadResourceFilesToContainer(). This data will be
            // processed by each of the tasks that are executed on the compute nodes within the pool.
            List<ResourceFile> inputFiles = await UploadFilesToContainerAsync(blobClient, inputContainerName, inputFilePaths);

            // Obtain a shared access signature that provides write access to the output container to which
            // the tasks will upload their output.
            string outputContainerSasUrl = GetContainerSasUrl(blobClient, outputContainerName, SharedAccessBlobPermissions.Write);

            // CREATE BATCH CLIENT / CREATE POOL / CREATE JOB / ADD TASKS
            // Create a Batch client and authenticate with shared key credentials.
            // The Batch client allows the app to interact with the Batch service.
            BatchSharedKeyCredentials sharedKeyCredentials = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(sharedKeyCredentials))
            {
                // Create the Batch pool, which contains the compute nodes that execute the tasks.
                await CreatePoolIfNotExistAsync(batchClient, PoolId);

                // Create the job that runs the tasks.
                await CreateJobAsync(batchClient, JobId, PoolId);

                // Create a collection of tasks and add them to the Batch job.
                // Provide a shared access signature for the tasks so that they can upload their output
                // to the Storage container.
                await AddTasksAsync(batchClient, JobId, inputFiles, outputContainerSasUrl);
            }

                // Print out timing info
                timer.Stop();
            Console.WriteLine();
            Console.WriteLine("Sample end: {0}", DateTime.Now);
            Console.WriteLine("Elapsed time: {0}", timer.Elapsed);
        }

        private static async Task CreateContainerIfNotExistAsync(CloudBlobClient blobClient, string containerName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            Console.WriteLine("Creating container [{0}].", containerName);
        }

        private static async Task<List<ResourceFile>> UploadFilesToContainerAsync(CloudBlobClient blobClient, string inputContainerName, List<string> filePaths)
        {
            List<ResourceFile> resourceFiles = new List<ResourceFile>();

            foreach (string filePath in filePaths)
            {
                resourceFiles.Add(await UploadResourceFileToContainerAsync(blobClient, inputContainerName, filePath));
            }

            return resourceFiles;
        }

        private static async Task<ResourceFile> UploadResourceFileToContainerAsync(CloudBlobClient blobClient, string containerName, string filePath)
        {
            Console.WriteLine("Uploading file {0} to container [{1}]...", filePath, containerName);
            string blobName = Path.GetFileName(filePath);
            var fileStream = File.OpenRead(filePath);

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            await blobData.UploadFromFileAsync(filePath);

            // Set the expiry time and permissions for the blob shared access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // Construct the SAS URL for blob
            string sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
            string blobSasUri = string.Format("{0}{1}", blobData.Uri, sasBlobToken);

            return ResourceFile.FromUrl(blobSasUri, blobName);
        }

        private static string GetContainerSasUrl(CloudBlobClient blobClient, string containerName, SharedAccessBlobPermissions permissions)
        {
            // Set the expiry time and permissions for the container access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately. Expiration is in 2 hours.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(2),
                Permissions = permissions
            };

            // Generate the shared access signature on the container, setting the constraints directly on the signature
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the URL string for the container, including the SAS token
            return string.Format("{0}{1}", container.Uri, sasContainerToken);
        }

        // BATCH CLIENT OPERATIONS - FUNCTION IMPLEMENTATIONS

        private static async Task CreatePoolIfNotExistAsync(BatchClient batchClient, string poolId)
        {
            CloudPool pool = null;
            try
            {
                Console.WriteLine("Creating pool [{0}]...", poolId);

                ImageReference imageReference = new ImageReference(
                    publisher: "MicrosoftWindowsServer",
                    offer: "WindowsServer",
                    sku: "2012-R2-Datacenter-smalldisk",
                    version: "latest");

                VirtualMachineConfiguration virtualMachineConfiguration =
                    new VirtualMachineConfiguration(
                        imageReference: imageReference,
                        nodeAgentSkuId: "batch.node.windows amd64");

                // Create an unbound pool. No pool is actually created in the Batch service until we call
                // CloudPool.Commit(). This CloudPool instance is therefore considered "unbound," and we can
                // modify its properties.
                pool = batchClient.PoolOperations.CreatePool(
                    poolId: PoolId,
                    virtualMachineSize: PoolVMSize,
                    virtualMachineConfiguration: virtualMachineConfiguration,
                    targetDedicatedComputeNodes: DedicatedNodeCount,
                    targetLowPriorityComputeNodes: LowPriorityNodeCount);

                // Specify the application and version to install on the compute nodes
                // This assumes that a Windows 64-bit zipfile of ffmpeg has been added to Batch account
                // with Application Id of "ffmpeg" and Version of "3.4".
                // Download the zipfile https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-3.4-win64-static.zip
                // to upload as application package
                pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                        ApplicationId = appPackageId,
                        Version = appPackageVersion
                    }
                };

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // Accept the specific error code PoolExists as that is expected if the pool already exists
                if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", poolId);
                }
                else
                {
                    throw; // Any other exception is unexpected
                }
            }
        }

        private static async Task CreateJobAsync(BatchClient batchClient, string jobId, string poolId)
        {
            Console.WriteLine("Creating job [{0}]...", jobId);

            CloudJob job = batchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation { PoolId = poolId };

            await job.CommitAsync();
        }

        private static async Task<List<CloudTask>> AddTasksAsync(BatchClient batchClient, string jobId, List<ResourceFile> inputFiles, string outputContainerSasUrl)
        {
            Console.WriteLine("Adding {0} tasks to job [{1}]...", inputFiles.Count, jobId);
            // Create a collection to hold the tasks added to the job:
            List<CloudTask> tasks = new List<CloudTask>();

            for (int i = 0 ; i < inputFiles.Count ; i++)
            {
                // Assign a task ID for each iteration
                string taskId = String.Format("Task{0}", i);

                // Define task command line to convert the video format from MP4 to MP3 using ffmpeg.
                // Note that ffmpeg syntax specifies the format as the file extension of the input file
                // and the output file respectively. In this case inputs are MP4.
                string appPath = String.Format("%AZ_BATCH_APP_PACKAGE_{0}#{1}%", appPackageId, appPackageVersion);
                string inputMediaFile = inputFiles[i].FilePath;
                string outputMediaFile = String.Format("{0}{1}", Path.GetFileNameWithoutExtension(inputMediaFile), ".mp3");
                string taskCommandLine = String.Format("cmd /c {0}\\ffmpeg-3.4-win64-static\\bin\\ffmpeg.exe -i {1} {2}", appPath, inputMediaFile, outputMediaFile);

                // Create a cloud task (with the task ID and command line) and add it to the task list
                CloudTask task = new CloudTask(taskId, taskCommandLine);
                task.ResourceFiles = new List<ResourceFile> { inputFiles[i] };

                // Task output file will be uploaded to the output container in Storage.
                List<OutputFile> outputFileList = new List<OutputFile>();
                OutputFileBlobContainerDestination outputContainer = new OutputFileBlobContainerDestination(outputContainerSasUrl);
                OutputFile outputFile = new OutputFile(outputMediaFile,
                    new OutputFileDestination(outputContainer),
                    new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));
                outputFileList.Add(outputFile);
                task.OutputFiles = outputFileList;
                tasks.Add(task);
            }

            // Call BatchClient.JobOperations.AddTask() to add the tasks as a collection rather than making a
            // separate call for each. Bulk task submission helps to ensure efficient underlying API
            // calls to the Batch service. 
            await batchClient.JobOperations.AddTaskAsync(jobId, tasks);
        }

    }
}

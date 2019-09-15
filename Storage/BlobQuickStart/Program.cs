using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlobQuickStart
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Blob Storage - .NET quickstart sample\n");

            // Run the examples asynchronously, wait for the results before proceeding
            ProcessAsync().GetAwaiter().GetResult();

            Console.WriteLine("Press any key to exit the sample application.");
            Console.ReadLine();
        }
        private static async Task ProcessAsync()
        {        
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonFile("settings.json")
                .AddJsonFile("settings.local.json")
                .Build();

            string storageConnectionString = configRoot.GetSection("StorageConnectionString").Value;

            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the CloudBlobClient that represents the 
            // Blob storage endpoint for the storage account.
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

            // Create a container called 'quickstartblobs' and 
            // append a GUID value to it to make the name unique.
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("quickstartblobs");
            await cloudBlobContainer.CreateIfNotExistsAsync();

            // Set the permissions so the blobs are public.
            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };
            await cloudBlobContainer.SetPermissionsAsync(permissions);

            // Create a file in your local MyDocuments folder to upload to a blob.
            string localPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFileName = "QuickStart_" + Guid.NewGuid().ToString() + ".txt";
            string sourceFile = Path.Combine(localPath, localFileName);
            // Write text to the file.
            File.WriteAllText(sourceFile, "Hello, World!");

            Console.WriteLine("Temp file = {0}", sourceFile);
            Console.WriteLine("Uploading to Blob storage as blob '{0}'", localFileName);

            // Get a reference to the blob address, then upload the file to the blob.
            // Use the value of localFileName for the blob name.
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(localFileName);
            await cloudBlockBlob.UploadFromFileAsync(sourceFile);

            // List the blobs in the container.
            Console.WriteLine("List blobs in container.");
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

            // Download the blob to a local file, using the reference created earlier.
            // Append the string "_DOWNLOADED" before the .txt extension so that you 
            // can see both files in MyDocuments.
            string destinationFile = sourceFile.Replace(".txt", "_DOWNLOADED.txt");
            Console.WriteLine("Downloading blob to {0}", destinationFile);
            await cloudBlockBlob.DownloadToFileAsync(destinationFile, FileMode.Create);
            
            // system properties and metadata
            await ReadContainerPropertiesAsync(cloudBlobContainer);
            await AddContainerMetadataAsync(cloudBlobContainer);
            await ReadContainerMetadataAsync(cloudBlobContainer);

            // list all containers
            await ListContainersWithPrefixAsync(cloudBlobClient, null);

            Console.WriteLine("Press the 'Enter' key to delete the example files, " +
    "example container, and exit the application.");
            Console.ReadLine();
            // Clean up resources. This includes the container and the two temp files.
            Console.WriteLine("Deleting the container");
            if (cloudBlobContainer != null)
            {
                // await cloudBlobContainer.DeleteIfExistsAsync();
            }
            Console.WriteLine("Deleting the source, and downloaded files");
            File.Delete(sourceFile);
            File.Delete(destinationFile);
        }

        private static async Task ListContainersWithPrefixAsync(CloudBlobClient blobClient, string prefix)
        {
            Console.WriteLine("List all containers beginning with prefix {0}, plus container metadata:", prefix);

            try
            {
                ContainerResultSegment resultSegment = null;
                BlobContinuationToken continuationToken = null;

                do
                {
                    // List containers beginning with the specified prefix, returning segments of 5 results each.
                    // Passing null for the maxResults parameter returns the max number of results (up to 5000).
                    // Requesting the container's metadata with the listing operation populates the metadata,
                    // so it's not necessary to also call FetchAttributes() to read the metadata.
                    resultSegment = await blobClient.ListContainersSegmentedAsync(
                        prefix, ContainerListingDetails.Metadata, 5, continuationToken, null, null);

                    // Enumerate the containers returned.
                    foreach (var container in resultSegment.Results)
                    {
                        Console.WriteLine("\tContainer:" + container.Name);

                        // Write the container's metadata keys and values.
                        foreach (var metadataItem in container.Metadata)
                        {
                            Console.WriteLine("\t\tMetadata key: " + metadataItem.Key);
                            Console.WriteLine("\t\tMetadata value: " + metadataItem.Value);
                        }
                    }

                    // Get the continuation token. If not null, get the next segment.
                    continuationToken = resultSegment.ContinuationToken;

                } while (continuationToken != null);
            }
            catch (StorageException e)
            {
                Console.WriteLine("HTTP error code {0} : {1}",
                                    e.RequestInformation.HttpStatusCode,
                                    e.RequestInformation.ErrorCode);
                Console.WriteLine(e.Message);
            }
        }

        private static async Task ReadContainerPropertiesAsync(CloudBlobContainer container)
        {
            try
            {
                // Fetch some container properties and write out their values.
                await container.FetchAttributesAsync();
                Console.WriteLine("Properties for container {0}", container.StorageUri.PrimaryUri);
                Console.WriteLine("Public access level: {0}", container.Properties.PublicAccess);
                Console.WriteLine("Last modified time in UTC: {0}", container.Properties.LastModified);
            }
            catch (StorageException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.RequestInformation.HttpStatusCode,
                                    e.RequestInformation.ErrorCode);
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        public static async Task AddContainerMetadataAsync(CloudBlobContainer container)
        {
            try
            {
                // Add some metadata to the container.
                container.Metadata.Add("docType", "textDocuments");
                container.Metadata["category"] = "guidance";

                // Set the container's metadata.
                await container.SetMetadataAsync();
            }
            catch (StorageException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.RequestInformation.HttpStatusCode,
                                    e.RequestInformation.ErrorCode);
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        public static async Task ReadContainerMetadataAsync(CloudBlobContainer container)
        {
            try
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
            catch (StorageException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.RequestInformation.HttpStatusCode,
                                    e.RequestInformation.ErrorCode);
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}

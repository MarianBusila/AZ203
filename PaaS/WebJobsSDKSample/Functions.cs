﻿using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace WebJobsSDKSample
{
    public class Functions
    {

        public static void ProcessQueueMessage(
            [QueueTrigger("queue")] string message, 
            [Blob("container/{queueTrigger}", FileAccess.Read)] Stream myBlob,
            ILogger logger)
        {
            logger.LogInformation($"Blob name: {message} \n Size: {myBlob.Length} bytes");

        }
    }
}

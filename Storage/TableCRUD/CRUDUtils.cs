using Microsoft.Azure.Cosmos.Table;
using System;
using System.Threading.Tasks;
using TableCRUD.Model;

namespace TableCRUD
{
    class CRUDUtils
    {
        public static async Task<CustomerEntity> InsertOrMergeEntityAsync(CloudTable table, CustomerEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                // Execute the operation
                TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                CustomerEntity insertedCustomer = result.Result as CustomerEntity;
                // Get the request units consumed by the current operation. RequestCharge of a TableResult is only applied to Azure Cosmos DB
                if (result.RequestCharge.HasValue)
                {
                    Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                }

                return insertedCustomer;
            }
            catch (StorageException ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                throw;
            }
        }
    }
}

using Microsoft.Azure.Cosmos.Table;

namespace TableCRUD.Model
{
    public class CustomerEntity : TableEntity
    {
        public CustomerEntity()
        {

        }

        public CustomerEntity(string lastName, string firstName)
        {
            PartitionKey = lastName;
            RowKey = firstName;
        }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }

    }
}

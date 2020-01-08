using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public class DbCustomerService : ICustomerService
    {
        private const int SIMULATE_DB_LOAD = 2000;

        private readonly IConfiguration configuration;

        public DbCustomerService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            if (SIMULATE_DB_LOAD > 0)
                await Task.Delay(SIMULATE_DB_LOAD);

            var connectionString = this.configuration.GetConnectionString("SqlConnectionString");

            using (var connection = new SqlConnection(connectionString))
            {
                Task openConnectionTask = connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM SalesLT.Customer";
                    command.CommandType = CommandType.Text;

                    await openConnectionTask.ConfigureAwait(false);

                    var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                    var result = await ReadCustomersAsync(reader).ConfigureAwait(false);

                    return result;
                }
            }
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            if (SIMULATE_DB_LOAD > 0)
                await Task.Delay(SIMULATE_DB_LOAD);

            var connectionString = this.configuration.GetConnectionString("SqlConnectionString");

            using (var connection = new SqlConnection(connectionString))
            {
                Task openConnectionTask = connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM SalesLT.Customer WHERE CustomerID = " + customerId;
                    command.CommandType = CommandType.Text;

                    await openConnectionTask.ConfigureAwait(false);

                    var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                    var result = await ReadCustomersAsync(reader).ConfigureAwait(false);

                    return result[0];
                }
            }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            if (SIMULATE_DB_LOAD > 0)
                await Task.Delay(SIMULATE_DB_LOAD);

            // Open to SQL injection attacks. Don't ever do this in production code.
            var commandText =
                " UPDATE SalesLT.Customer " +
                " SET " +
                "    NameStyle = " + (customer.NameStyle ? 1 : 0) + ", " +
                "    Title = '" + customer.Title + "', " +
                "    FirstName = '" + customer.FirstName + "', " +
                "    MiddleName = '" + customer.MiddleName + "', " +
                "    LastName = '" + customer.LastName + "', " +
                "    Suffix = '" + customer.Suffix + "', " +
                "    CompanyName = '" + customer.CompanyName + "', " +
                "    SalesPerson = '" + customer.SalesPerson + "', " +
                "    EmailAddress = '" + customer.EmailAddress + "', " +
                "    Phone = '" + customer.Phone + "', " +
                "    PasswordHash = '" + customer.PasswordHash + "', " +
                "    PasswordSalt = '" + customer.PasswordSalt + "', " +
                "    ModifiedDate = '" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "' " +
                " WHERE " +
                "    CustomerId = " + customer.CustomerID;

            var connectionString = this.configuration.GetConnectionString("SqlConnectionString");

            using (var connection = new SqlConnection(connectionString))
            {
                Task openConnectionTask = connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.CommandType = CommandType.Text;

                    await openConnectionTask.ConfigureAwait(false);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }


        public static async Task<List<Customer>> ReadCustomersAsync(SqlDataReader reader)
        {
            List<Customer> customers = new List<Customer>();

            int CustomerIDOrdinal = reader.GetOrdinal("CustomerID");
            int NameStyleOrdinal = reader.GetOrdinal("NameStyle");
            int TitleOrdinal = reader.GetOrdinal("Title");
            int FirstNameOrdinal = reader.GetOrdinal("FirstName");
            int MiddleNameOrdinal = reader.GetOrdinal("MiddleName");
            int LastNameOrdinal = reader.GetOrdinal("LastName");
            int SuffixOrdinal = reader.GetOrdinal("Suffix");
            int CompanyNameOrdinal = reader.GetOrdinal("CompanyName");
            int SalesPersonOrdinal = reader.GetOrdinal("SalesPerson");
            int EmailAddressOrdinal = reader.GetOrdinal("EmailAddress");
            int PhoneOrdinal = reader.GetOrdinal("Phone");
            int PasswordHashOrdinal = reader.GetOrdinal("PasswordHash");
            int PasswordSaltOrdinal = reader.GetOrdinal("PasswordSalt");
            int RowGuidOrdinal = reader.GetOrdinal("RowGuid");
            int ModifiedDateOrdinal = reader.GetOrdinal("ModifiedDate");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                Customer customer = new Customer()
                {
                    CustomerID = reader.GetInt32(CustomerIDOrdinal),
                    NameStyle = reader.GetBoolean(NameStyleOrdinal),
                    Title = reader.IsDBNull(TitleOrdinal) ? null : reader.GetString(TitleOrdinal),
                    FirstName = reader.IsDBNull(FirstNameOrdinal) ? null : reader.GetString(FirstNameOrdinal),
                    MiddleName = reader.IsDBNull(MiddleNameOrdinal) ? null : reader.GetString(MiddleNameOrdinal),
                    LastName = reader.IsDBNull(LastNameOrdinal) ? null : reader.GetString(LastNameOrdinal),
                    Suffix = reader.IsDBNull(SuffixOrdinal) ? null : reader.GetString(SuffixOrdinal),
                    CompanyName = reader.IsDBNull(CompanyNameOrdinal) ? null : reader.GetString(CompanyNameOrdinal),
                    SalesPerson = reader.IsDBNull(SalesPersonOrdinal) ? null : reader.GetString(SalesPersonOrdinal),
                    EmailAddress = reader.IsDBNull(EmailAddressOrdinal) ? null : reader.GetString(EmailAddressOrdinal),
                    Phone = reader.IsDBNull(PhoneOrdinal) ? null : reader.GetString(PhoneOrdinal),
                    PasswordHash = reader.GetString(PasswordHashOrdinal),
                    PasswordSalt = reader.GetString(PasswordSaltOrdinal),
                    RowGuid = reader.GetGuid(RowGuidOrdinal),
                    ModifiedDate = reader.GetDateTime(ModifiedDateOrdinal),
                };
                customers.Add(customer);
            }

            return customers;
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace CustomerApiFunctions
{
    public static class CustomersAPI
    {
        [FunctionName("GetAllCustomers")]
        public static async Task<IActionResult> GetAllCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customer")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("HTTP request to GetAllCustomers function.");

            var config = CreateConfiguration(context);

            var executor = new SqlExecutor(config);
            var customers = await executor.Execute(
                "SELECT * FROM SalesLT.Customer",
                isReadonly: true,
                CustomerDAL.ReadCustomerAsync);

            return new JsonResult(customers);
        }

        [FunctionName("GetCustomerById")]
        public static async Task<IActionResult> GetCustomerById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customer/{id}")] HttpRequest req,
            string id,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("HTTP request to GetCustomerById function with id={0}.", id);

            var config = CreateConfiguration(context);

            int customerId = int.Parse(id);

            var executor = new SqlExecutor(config);
            var customers = await executor.Execute(
                "SELECT * FROM SalesLT.Customer WHERE CustomerID = " + customerId,
                isReadonly: true,
                readCallbackAsync: CustomerDAL.ReadCustomerAsync);

            return new JsonResult(customers[0]);
        }

        [FunctionName("CreateCustomer")]
        public static async Task<IActionResult> CreateCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customer")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("HTTP request to CreateCustomer function.");

            var config = CreateConfiguration(context);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(requestBody);

            // Never do this in production. It is open to SQL injection attacks.
            var commandText =
                "INSERT INTO SalesLT.Customer " +
                "(" +
                "    NameStyle," +
                "    Title," +
                "    FirstName," +
                "    MiddleName," +
                "    LastName," +
                "    Suffix," +
                "    CompanyName," +
                "    SalesPerson," +
                "    EmailAddress," +
                "    Phone," +
                "    PasswordHash," +
                "    PasswordSalt," +
                "    ModifiedDate" +
                ") " +
                "OUTPUT" +
                "    inserted.CustomerID " +
                "VALUES" +
                "(" +
                "      " + (customer.NameStyle ? 1 : 0) +
                "    ,'" + customer.Title + "'" +
                "    ,'" + customer.FirstName + "'" +
                "    ,'" + customer.MiddleName + "'" +
                "    ,'" + customer.LastName + "'" +
                "    ,'" + customer.Suffix + "'" +
                "    ,'" + customer.CompanyName + "'" +
                "    ,'" + customer.SalesPerson + "'" +
                "    ,'" + customer.EmailAddress + "'" +
                "    ,'" + customer.Phone + "'" +
                "    ,'" + customer.PasswordHash + "'" +
                "    ,'" + customer.PasswordSalt + "'" +
                "    ,'" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "'" +
                ")";

            var executor = new SqlExecutor(config);
            int id = await executor.Execute<int>(commandText, isReadonly: false);

            return new JsonResult(id);
        }

        [FunctionName("UpdateCustomer")]
        public static async Task<IActionResult> UpdateCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "customer/{id}")] HttpRequest req,
            string id,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("HTTP request to UpdateCustomer function with id={0}.", id);

            var config = CreateConfiguration(context);

            int customerId = int.Parse(id);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(requestBody);

            // Never do this in production. It is open to SQL injection attacks.
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
                "    CustomerId = " + customerId;

            var executor = new SqlExecutor(config);
            await executor.Execute(commandText, isReadonly: false);

            return new OkResult();
        }

        [FunctionName("DeleteCustomer")]
        public static async Task<IActionResult> DeleteCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "customer/{id}")] HttpRequest req,
            string id,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("HTTP request to DeleteCustomer function with id={0}.", id);

            var config = CreateConfiguration(context);

            int customerId = int.Parse(id);

            var commandText =
                "DELETE FROM SalesLT.Customer WHERE CustomerId = " + customerId;

            var executor = new SqlExecutor(config);
            await executor.Execute(commandText, isReadonly: false);

            return new OkResult();
        }


        private static IConfiguration CreateConfiguration(ExecutionContext context)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return config;
        }
    }
}

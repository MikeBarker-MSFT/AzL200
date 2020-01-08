using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly DbCustomerService dbService;
        private readonly RedisCustomerService redisService;

        public CustomerService(
            IConfiguration configuration)
        {
            this.dbService = new DbCustomerService(configuration);
            this.redisService = new RedisCustomerService(configuration);
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var result = await this.dbService.GetAllCustomersAsync();

            return result;
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            var customer = await this.redisService.GetCustomerAsync(customerId);

            if (customer == null)
            {
                customer = await this.dbService.GetCustomerAsync(customerId);
                await redisService.CacheCustomers(new[] { customer });
            }

            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            // Write to both the DB and Redis services
            var dbpdateTask = this.dbService.UpdateCustomerAsync(customer);
            var redisUpdateTask = this.redisService.UpdateCustomerAsync(customer);

            await redisUpdateTask;
            await dbpdateTask;
        }
    }
}

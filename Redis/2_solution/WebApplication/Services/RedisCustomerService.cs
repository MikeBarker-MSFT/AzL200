using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public class RedisCustomerService : ICustomerService
    {
        private readonly IConfiguration configuration;
        private ConnectionMultiplexer redisConnectionMultiplexer;

        public RedisCustomerService(
            IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        private ConnectionMultiplexer RedisConnectionMultiplexer
        {
            get
            {
                if (redisConnectionMultiplexer == null)
                {
                    string connectionString = this.configuration.GetConnectionString("RedisConnectionString");

                    redisConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                }

                return redisConnectionMultiplexer;
            }
        }


        Task<IEnumerable<Customer>> ICustomerService.GetAllCustomersAsync()
        {
            throw new NotSupportedException();
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            string key = GetCustomerKey(customerId);

            string customerJson = await redisDatabase.StringGetAsync(key);

            Customer customer = null;
            if (customerJson != null)
            {
                customer = JsonConvert.DeserializeObject<Customer>(customerJson);
            }

            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            string customerJson = JsonConvert.SerializeObject(customer);

            string key = GetCustomerKey(customer.CustomerID);

            await redisDatabase.StringSetAsync(key, customerJson);
        }

        private static string GetCustomerKey(long customerId)
        {
            return $"Customer_{customerId}";
        }

        public async Task CacheCustomers(IEnumerable<Customer> customers)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            var pairs = customers
                .Select(customer =>
                {
                    string customerJson = JsonConvert.SerializeObject(customer);
                    string key = GetCustomerKey(customer.CustomerID);

                    return new KeyValuePair<RedisKey, RedisValue>(key, customerJson);
                })
                .ToArray();

            await redisDatabase.StringSetAsync(pairs);
        }
    }
}

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HttpClient client;

        public CustomerService(IConfiguration config)
        {
            var baseAddress = config.GetValue<string>("CustomerUrlBaseAddress");
            var functionKey = config.GetValue<string>("CustomerFunctionKey");

            this.client = new HttpClient();
            this.client.BaseAddress = new Uri(baseAddress);
            this.client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            string url = "api/customer";

            HttpResponseMessage response = await this.client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var customers = JsonConvert.DeserializeObject<List<Customer>>(responseBody);

            return customers;
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            string url = string.Format("api/customer/{0}", customerId);

            HttpResponseMessage response = await this.client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var customer = JsonConvert.DeserializeObject<Customer>(responseBody);

            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            string url = string.Format("api/customer/{0}", customer.CustomerID);

            HttpResponseMessage response = await this.client.PutAsJsonAsync(url, customer);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteCustomerAsync(long customerId)
        {
            string url = string.Format("api/customer/{0}", customerId);

            HttpResponseMessage response = await this.client.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
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
        private readonly AsyncPolicy executionPolicy;

        public CustomerService(IConfiguration config)
        {
            var baseAddress = config.GetValue<string>("CustomerUrlBaseAddress");
            var functionKey = config.GetValue<string>("CustomerFunctionKey");

            this.client = new HttpClient();
            this.client.BaseAddress = new Uri(baseAddress);
            this.client.DefaultRequestHeaders.Add("x-functions-key", functionKey);

            AsyncPolicy retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) });

            AsyncPolicy circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(10, TimeSpan.FromSeconds(30));

            this.executionPolicy = Policy.WrapAsync(
                retryPolicy,
                circuitBreakerPolicy);
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            string url = "api/customer";

            HttpResponseMessage response = await executionPolicy
                    .ExecuteAsync(async () =>
                    {
                        HttpResponseMessage resp = await this.client.GetAsync(url);
                        resp.EnsureSuccessStatusCode();

                        return resp;
                    });

            string responseBody = await response.Content.ReadAsStringAsync();

            var customers = JsonConvert.DeserializeObject<List<Customer>>(responseBody);

            return customers;
        }

        public async Task<Customer> GetCustomerAsync(long customerId)
        {
            string url = string.Format("api/customer/{0}", customerId);

            HttpResponseMessage response = await executionPolicy
                    .ExecuteAsync(async () =>
                    {
                        HttpResponseMessage resp = await this.client.GetAsync(url);
                        resp.EnsureSuccessStatusCode();

                        return resp;
                    });

            string responseBody = await response.Content.ReadAsStringAsync();

            var customer = JsonConvert.DeserializeObject<Customer>(responseBody);

            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            string url = string.Format("api/customer/{0}", customer.CustomerID);

            HttpResponseMessage response = await executionPolicy
                    .ExecuteAsync(async () =>
                    {
                        HttpResponseMessage resp = await this.client.PutAsJsonAsync(url, customer);
                        resp.EnsureSuccessStatusCode();

                        return resp;
                    });
        }

        public async Task DeleteCustomerAsync(long customerId)
        {
            string url = string.Format("api/customer/{0}", customerId);

            HttpResponseMessage response = await executionPolicy
                    .ExecuteAsync(async () =>
                    {
                        HttpResponseMessage resp = await this.client.DeleteAsync(url);
                        resp.EnsureSuccessStatusCode();

                        return resp;
                    });
        }
    }
}

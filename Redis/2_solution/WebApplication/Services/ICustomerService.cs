using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication.DataModels;

namespace WebApplication.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllCustomersAsync();

        Task<Customer> GetCustomerAsync(long customerId);

        Task UpdateCustomerAsync(Customer customer);
    }
}

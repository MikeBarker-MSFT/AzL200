using System.Collections.Generic;
using WebApplication.DataModels;

namespace WebApplication.Models
{
    public class ViewAllCustomersViewModel
    {
        public IReadOnlyList<Customer> Customers { get; set; }
    }
}

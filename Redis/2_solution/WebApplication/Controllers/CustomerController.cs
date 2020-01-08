using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.DataModels;
using WebApplication.Models;
using WebApplication.Services;

namespace WebApplication.Controllers
{
    public class CustomerController : Controller
    {
        public CustomerController(
            ICustomerService customerService)
        {
            this.CustomerService = customerService;
        }

        private ICustomerService CustomerService { get; }


        public async Task<IActionResult> Index()
        {
            var allCustomers = await this.CustomerService.GetAllCustomersAsync();

            var model = new ViewAllCustomersViewModel
            {
                Customers = allCustomers.ToList(),
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            long customerId = long.Parse(id);

            var customer = await this.CustomerService.GetCustomerAsync(customerId);

            var model = new EditCustomerViewModel
            {
                CustomerID = customer.CustomerID,
                NameStyle = customer.NameStyle,
                Title = customer.Title,
                FirstName = customer.FirstName,
                MiddleName = customer.MiddleName,
                LastName = customer.LastName,
                Suffix = customer.Suffix,
                CompanyName = customer.CompanyName,
                SalesPerson = customer.SalesPerson,
                EmailAddress = customer.EmailAddress,
                Phone = customer.Phone,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditCustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                long customerId = model.CustomerID;

                Customer customer = await this.CustomerService.GetCustomerAsync(customerId);

                customer.NameStyle = model.NameStyle;
                customer.Title = model.Title;
                customer.FirstName = model.FirstName;
                customer.MiddleName = model.MiddleName;
                customer.LastName = model.LastName;
                customer.Suffix = model.Suffix;
                customer.CompanyName = model.CompanyName;
                customer.SalesPerson = model.SalesPerson;
                customer.EmailAddress = model.EmailAddress;
                customer.Phone = model.Phone;

                await this.CustomerService.UpdateCustomerAsync(customer);
                
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}

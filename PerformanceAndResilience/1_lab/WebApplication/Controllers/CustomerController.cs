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

                bool hasChanges = false;

                if (!Equals(customer.NameStyle, model.NameStyle))
                {
                    hasChanges = true;
                    customer.NameStyle = model.NameStyle;
                }

                if (!Equals(customer.Title, model.Title))
                {
                    hasChanges = true;
                    customer.Title = model.Title;
                }

                if (!Equals(customer.FirstName, model.FirstName))
                {
                    hasChanges = true;
                    customer.FirstName = model.FirstName;
                }

                if (!Equals(customer.MiddleName, model.MiddleName))
                {
                    hasChanges = true;
                    customer.MiddleName = model.MiddleName;
                }

                if (!Equals(customer.LastName, model.LastName))
                {
                    hasChanges = true;
                    customer.LastName = model.LastName;
                }

                if (!Equals(customer.Suffix, model.Suffix))
                {
                    hasChanges = true;
                    customer.Suffix = model.Suffix;
                }

                if (!Equals(customer.CompanyName, model.CompanyName))
                {
                    hasChanges = true;
                    customer.CompanyName = model.CompanyName;
                }

                if (!Equals(customer.SalesPerson, model.SalesPerson))
                {
                    hasChanges = true;
                    customer.SalesPerson = model.SalesPerson;
                }

                if (!Equals(customer.EmailAddress, model.EmailAddress))
                {
                    hasChanges = true;
                    customer.EmailAddress = model.EmailAddress;
                }

                if (!Equals(customer.Phone, model.Phone))
                {
                    hasChanges = true;
                    customer.Phone = model.Phone;
                }


                if (hasChanges)
                {
                    await this.CustomerService.UpdateCustomerAsync(customer);
                }

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            long customerId = long.Parse(id);

            await this.CustomerService.DeleteCustomerAsync(customerId);

            return RedirectToAction(nameof(Index));
        }
    }
}

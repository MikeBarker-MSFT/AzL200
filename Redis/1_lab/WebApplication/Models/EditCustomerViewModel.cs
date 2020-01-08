using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApplication.Models
{
    public class EditCustomerViewModel
    {
        [Required]
        public int CustomerID { get; set; }

        [DisplayName("Name style")]
        public bool NameStyle { get; set; }

        public string Title { get; set; }

        [DisplayName("First name")]
        public string FirstName { get; set; }

        [DisplayName("Middle name")]
        public string MiddleName { get; set; }

        [Required]
        [DisplayName("Last name")]
        public string LastName { get; set; }

        [DisplayName("Suffix")]
        public string Suffix { get; set; }

        [DisplayName("Company name")]
        public string CompanyName { get; set; }

        [DisplayName("Sales person")]
        public string SalesPerson { get; set; }

        [EmailAddress]
        [DisplayName("Email address")]
        public string EmailAddress { get; set; }

        [Phone]
        [DisplayName("Phone number")]
        public string Phone { get; set; }
    }
}

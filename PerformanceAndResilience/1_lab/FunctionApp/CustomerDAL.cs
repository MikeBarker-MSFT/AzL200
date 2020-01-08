using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace CustomerApiFunctions
{
    public static class CustomerDAL
    {
        public static async Task<List<Customer>> ReadCustomerAsync(SqlDataReader reader)
        {
            List<Customer> customers = new List<Customer>();

            int CustomerIDOrdinal = reader.GetOrdinal("CustomerID");
            int NameStyleOrdinal = reader.GetOrdinal("NameStyle");
            int TitleOrdinal = reader.GetOrdinal("Title");
            int FirstNameOrdinal = reader.GetOrdinal("FirstName");
            int MiddleNameOrdinal = reader.GetOrdinal("MiddleName");
            int LastNameOrdinal = reader.GetOrdinal("LastName");
            int SuffixOrdinal = reader.GetOrdinal("Suffix");
            int CompanyNameOrdinal = reader.GetOrdinal("CompanyName");
            int SalesPersonOrdinal = reader.GetOrdinal("SalesPerson");
            int EmailAddressOrdinal = reader.GetOrdinal("EmailAddress");
            int PhoneOrdinal = reader.GetOrdinal("Phone");
            int PasswordHashOrdinal = reader.GetOrdinal("PasswordHash");
            int PasswordSaltOrdinal = reader.GetOrdinal("PasswordSalt");
            int RowGuidOrdinal = reader.GetOrdinal("RowGuid");
            int ModifiedDateOrdinal = reader.GetOrdinal("ModifiedDate");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                Customer customer = new Customer()
                {
                    CustomerID = reader.GetInt32(CustomerIDOrdinal),
                    NameStyle = reader.GetBoolean(NameStyleOrdinal),
                    Title = reader.IsDBNull(TitleOrdinal) ? null : reader.GetString(TitleOrdinal),
                    FirstName = reader.IsDBNull(FirstNameOrdinal) ? null : reader.GetString(FirstNameOrdinal),
                    MiddleName = reader.IsDBNull(MiddleNameOrdinal) ? null : reader.GetString(MiddleNameOrdinal),
                    LastName = reader.IsDBNull(LastNameOrdinal) ? null : reader.GetString(LastNameOrdinal),
                    Suffix = reader.IsDBNull(SuffixOrdinal) ? null : reader.GetString(SuffixOrdinal),
                    CompanyName = reader.IsDBNull(CompanyNameOrdinal) ? null : reader.GetString(CompanyNameOrdinal),
                    SalesPerson = reader.IsDBNull(SalesPersonOrdinal) ? null : reader.GetString(SalesPersonOrdinal),
                    EmailAddress = reader.IsDBNull(EmailAddressOrdinal) ? null : reader.GetString(EmailAddressOrdinal),
                    Phone = reader.IsDBNull(PhoneOrdinal) ? null : reader.GetString(PhoneOrdinal),
                    PasswordHash = reader.GetString(PasswordHashOrdinal),
                    PasswordSalt = reader.GetString(PasswordSaltOrdinal),
                    RowGuid = reader.GetGuid(RowGuidOrdinal),
                    ModifiedDate = reader.GetDateTime(ModifiedDateOrdinal),
                };
                customers.Add(customer);
            }

            return customers;
        }
    }
}

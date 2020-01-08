using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Linq;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace CMDApp
{
    class Program
    {
        private static string graphAPIUrl = "https://graph.microsoft.com/v1.0/users";
        private static string aad = ConfigurationManager.AppSettings["ida:AAD"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string certificateName = ConfigurationManager.AppSettings["ida:CertificateName"];
        private static string authority = String.Format(CultureInfo.InvariantCulture, aad, tenant);
        private static string audienceUri = ConfigurationManager.AppSettings["ida:AudienceUri"];
        private static HttpClient httpClient = new HttpClient();
        
        static void Main(string[] args)
        {
            var result = CallGraphAPI().Result;
            Console.WriteLine(result);

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        static async Task<string> CallGraphAPI()
        {
            AuthenticationResult result = await GetAccessToken(audienceUri);
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = await httpClient.GetAsync(graphAPIUrl);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<AuthenticationResult> GetAccessToken(string resourceId)
        {
            AuthenticationResult result = null;
                    
            X509Certificate2 cert = ReadCertificateFromStore(certificateName);
            ClientAssertionCertificate certCred = new ClientAssertionCertificate(clientId, cert);

            AuthenticationContext authContext = new AuthenticationContext(authority);
            try
            {
                result = await authContext.AcquireTokenAsync(resourceId, certCred);
            }
            catch (AdalException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return result;
        }

        private static X509Certificate2 ReadCertificateFromStore(string certificateName)
        {
            X509Certificate2 certificate = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = store.Certificates;                   
            X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);            
            X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certificateName, false);
            certificate = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            store.Close();
            return certificate;
        }    
    }
}

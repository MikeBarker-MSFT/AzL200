using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace WebApplication.Controllers
{
    public class CacheController : Controller
    {
        private readonly IDistributedCache cache;

        public CacheController(
            IDistributedCache cache)
        {
            this.cache = cache;
        }

        public async Task<IActionResult> Read()
        {
            // Read from the cache
            byte[] buffer = await cache.GetAsync("DistributedCacheKey");

            string message = null;
            if (buffer != null)
            {
                message = Encoding.UTF8.GetString(buffer);
            }

            ViewBag.Message = message ?? "- cache miss -";
            return View();
        }

        public IActionResult Update()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Update(string cacheValue)
        {
            var buffer = Encoding.UTF8.GetBytes(cacheValue);

            // Set a TTL of 20 seconds
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20)
            };

            // Write to the cache
            await cache.SetAsync("DistributedCacheKey", buffer, options);

            return RedirectToAction("Read");
        }

        public async Task<IActionResult> SessionState()
        {
            // Get the session data
            await HttpContext.Session.LoadAsync();

            string sessionTime = HttpContext.Session.GetString("SessionTime");
            if (sessionTime == null)
            {
                sessionTime = DateTime.Now.ToString("hh:mm:ss.fff");

                // Store the session time in
                HttpContext.Session.SetString("SessionTime", sessionTime);

                await HttpContext.Session.CommitAsync();
            }

            ViewBag.Message = sessionTime;
            return View();
        }
    }
}
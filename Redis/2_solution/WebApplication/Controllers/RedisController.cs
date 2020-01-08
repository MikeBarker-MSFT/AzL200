using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace RedisWeb.Controllers
{
    public class RedisController : Controller
    {
        private readonly IConfiguration configuration;
        private ConnectionMultiplexer redisConnectionMultiplexer;

        public RedisController(
            IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private ConnectionMultiplexer RedisConnectionMultiplexer
        {
            get
            {
                //
                // Establish the connection to Redis once and reuse it throughout the application.
                //
                if (redisConnectionMultiplexer == null)
                {
                    string connectionString = this.configuration.GetConnectionString("RedisConnectionString");

                    redisConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                }

                return redisConnectionMultiplexer;
            }
        }


        public async Task<IActionResult> Read()
        {
            //
            // Retrieve a string with the key "MyFirstKey" from the Redis cache.
            //
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            string myFirstCachedValue = await redisDatabase.StringGetAsync("MyFirstKey");

            ViewBag.Message = myFirstCachedValue ?? "- cache miss -";
            return View();
        }

        public IActionResult Update()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Update(string cacheValue)
        {
            //
            // Set the string with the key "MyFirstKey" in the Redis cache, specifying a TTL of 20 seconds.
            //
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            await redisDatabase.StringSetAsync("MyFirstKey", cacheValue, TimeSpan.FromSeconds(20));

            return RedirectToAction("Read");
        }

        public async Task<IActionResult> ByteArray()
        {
            byte[] buffer = GenerateRandomByteArray();

            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            // Set the byte array in Redis
            await redisDatabase.StringSetAsync("ByteArrayKey", buffer, TimeSpan.FromSeconds(20));

            // Retrieve the byte array from Redis
            byte[] retrievedArray = await redisDatabase.StringGetAsync("ByteArrayKey");

            ViewBag.ByteArray = retrievedArray;
            return View();
        }

        private static readonly Random random = new Random();

        private static byte[] GenerateRandomByteArray()
        {
            int length = random.Next(10, 50);
            byte[] buffer = new byte[length];
            random.NextBytes(buffer);

            return buffer;
        }

        public async Task<IActionResult> List(string popValue)
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            // Read the full list
            // Notice that -1 means read to the final index in the list
            var list = await redisDatabase.ListRangeAsync("ListKey", 0, -1);

            ViewBag.PopValue = popValue;
            ViewBag.List = list.ToStringArray();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Pop()
        {
            IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

            // Pop a value off the list
            string popValue = await redisDatabase.ListLeftPopAsync("ListKey");

            //
            // TODO: Do some processing with the value
            //

            return RedirectToAction("List", new { popValue = popValue });
        }

        [HttpPost]
        public async Task<IActionResult> Push(string pushValue)
        {
            if (!string.IsNullOrEmpty(pushValue))
            {
                IDatabase redisDatabase = this.RedisConnectionMultiplexer.GetDatabase();

                // Push a value to the list
                await redisDatabase.ListRightPushAsync("ListKey", pushValue);
            }

            return RedirectToAction("List");
        }
    }
}
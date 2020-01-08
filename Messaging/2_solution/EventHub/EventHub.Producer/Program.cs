using Microsoft.Azure.EventHubs;
using System;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Producer
{
    public class Program
    {
        private static EventHubClient eventHubClient;
        private const string EventHubConnectionString = "<event hub connection string>";
        private const string EventHubName = "messaging_lab";

        private static bool exit = false;


        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
            {
                EntityPath = EventHubName
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            Task sendMessageTask = SendMessagesToEventHub();

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();

            exit = true;

            Task.WaitAll(sendMessageTask);

            await eventHubClient.CloseAsync();
        }

        private static async Task SendMessagesToEventHub()
        {
            int i = 0;

            while (!exit)
            {
                try
                {
                    var message = $"Message {i}";
                    Console.WriteLine($"Sending message: {message}");
                    await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)), "MyPartition");
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
                }

                await Task.Delay(10);
                i++;
            }
        }
    }
}
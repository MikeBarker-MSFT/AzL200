using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Threading.Tasks;

namespace EventHub.Consumer
{
    public class Program
    {
        private const string EventHubConnectionString = "<event hub connection string>";
        private const string EventHubName = "messaging_lab";
        private const string StorageConnectionString = "<storage ccount connection string>";
        private const string StorageContainerName = "checkpointcontainer";

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("");

            Console.WriteLine("Registering EventProcessor...");

            var eventProcessorHost = new EventProcessorHost(
                EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                EventHubConnectionString,
                StorageConnectionString,
                StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

            Console.WriteLine("Receiving. Press ENTER to stop worker.");
            Console.ReadLine();

            // Disposes of the Event Processor Host
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }
    }
}
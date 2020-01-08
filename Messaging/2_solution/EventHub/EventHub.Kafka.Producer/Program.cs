using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace EventHub.Kafka.Consumer
{
    class Program
    {
        private const string EventHubNamespace = "<event hub namespace>";
        private const string EventHubAccessKey = "<event hub access key>";
        private const string EventHubName = "messaging_lab";

        public static async Task Main(string[] args)
        {
            string eventHubConnectionString = $"Endpoint=sb://{EventHubNamespace}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={EventHubAccessKey}";

            var config = new ProducerConfig
            {
                BootstrapServers = $"{EventHubNamespace}.servicebus.windows.net:9093",
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "$ConnectionString",
                SaslPassword = eventHubConnectionString,
            };

            using (var p = new ProducerBuilder<Null, string>(config).Build())
            {
                try
                {
                    var dr = await p.ProduceAsync(EventHubName, new Message<Null, string> { Value = "test" });
                    Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            }
        }
    }
}


using System;
using System.Threading;
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

            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = $"{EventHubNamespace}.servicebus.windows.net:9093",
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "$ConnectionString",
                SaslPassword = eventHubConnectionString,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                c.Subscribe(EventHubName);

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close();
                }
            }
        }
    }
}
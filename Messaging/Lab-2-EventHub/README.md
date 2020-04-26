

# Lab 2: Exploring Event Hub

In this lab we will explore Event Hub. We will explore partitioning, multiple readers in a consumer group, and connecting using the Kafka API.


## Create a Rsource Group
If you haven't already, create a resource group for this lab.
1. Sign into the [Azure Portal](https://portal.azure.com/)
1. In the far-left menu select **Resource groups**
1. Click **+ Add**
    + _Name:_ `az{your_id}-messaging-rg`
    + _Region:_ `West Europe`
    + Click **Review + create**
    + Click **Create**


## Create a Event Hub namespace
A namespace is the logical container for Event Hubs.

Within the portal:
1. From the left-hand menu click on **+ Create a resource**
1. Search for and select `Event Hubs`
1. Click **Create**
1. In the Create namespace blade:
    + _Name_: `az{your_id}-messaging-eventhub`
    + _Pricing tier:_ `Standard` :one:
    + _Enable Kafka_: `Enabled`
    + _Resource group:_ `az{your_id}-messaging-rg`
    + _Location:_ `West Europe`
    + _Throughput Units:_ `1`
1. Click **Create**

The Event Hub namespace will take approximately 1 minute to be created.

:one: The Kafka functionality will be explored in "Going further", and requires an Event Hub in the `Standard` tier.  If you do not plan on exploring this section, you may select `Basic`.

## Create an Event Hub
1. From the Event Hub Namespace blade create a Event Hub. At the top of the main blade select **+ Event Hub**
    + _Name:_ `messaging_lab`
    + _Partitions:_ `3`
    + _Message Retention:_ `1`
    + _Capture:_`Off`
    + Click **Create**
1. Now retrieve a connection string for your event hub namespace, and make note of it. You will need to paste this into the chat application.
    1. In the Left-hand menu select **Settings** > **Shared access policies**
    1. Select the **RootManageSharedAccessKey**.
    1. From the Pop-up copy the value of **Connection string-primary key**
        * This should start with `Endpoint=sb://az{your_id}-messaging-eventhub.servicebus.windows.net...`
    1. Also make a note of your Event Hub's name (i.e. `messaging_lab`), as you will need this too.

You may have noticed that the connection string includes the "`servicebus`" in the URL. This is due to historic reasons. Event Hubs, Azure Relays and Service Bus used to be all encapsulated under the name "Service Bus", until they were each split-out into their own products.

## Create a Storage Account to record consumer state
Event Hub consumers need a durable store to record its state can be stored (known as _Checkpointing_). In this exercise we'll use the SDK's in-built support for Azure storage.

Within the portal:
1. From the left-hand menu click on **+ Create a resource**
1. Search for and select `Storage Account`
1. Click **Create**
1. In the Create namespace blade:
    + _Resource group:_ `az{your_id}-messaging-rg`
    + _Name_: `az{your_id}eventhubstore`
    + _Location:_ `West Europe`
    + _Replication:_ `Locally-redundant storage (LRS)`
1. Click **Review + create**
1. Click **Create**

Make a note of the storage account's access key (to be used later):
1. In the storage account blade, **Settings** > **Access keys**
1. Copy the **Connection string** of _key1_.


## Create a message producer application

In Visual Studio:
1. Create a new .NET Console project.
    + File -> New Project
    + From the left menu **Visual C#** > **.NET Core**
    + Choose **Console App (.NET Core)**
    + Provide a name for your project, e.g. `EventHub.Producer`
    + Provide a name for your solution, e.g. `EventHub`
    + Click **OK**
1. Add the Azure Event Hub NuGet package
    + Right-click the project in the Solution Explorer
    + Manage NuGet Packages...
    + Change to the **Browse** tab at the tab of the window
    + Search for `Microsoft.Azure.EventHubs`
    + Select the package and click Install
    + Click **OK** in the Preview Changes window, and accept the licenses.
1. Open the **Program.cs** file, and paste the following code:
```cs
using Microsoft.Azure.EventHubs;
using System;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Producer
{
    public class Program
    {
        private static EventHubClient eventHubClient;
        private const string EventHubConnectionString = "<event hub namespace connection string>";
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
                    string message = $"Message {i}";
                    Console.WriteLine($"Sending message: {message}");
                    await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
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
```

Substitute you event hub namespace connection string and event hub name.
Read through the code and ensure you understand the structure of how the application works.

## Create a message consumer application

1. Right-click the solution and Select **Add** > **New project...**
    + Choose **Console App (.NET Core)**
    + Provide a name for your project, e.g. `Consumer`
    + Click **OK**
1. Add the Azure Event Hub NuGet package
    + Right-click the project in the Solution Explorer
    + Manage NuGet Packages...
    + Change to the **Browse** tab at the tab of the window
    + Search for `Microsoft.Azure.EventHubs`
    + Select the package and click Install
    + Click **OK** in the Preview Changes window, and accept the licenses.
    + Repeat the above steps to install `Microsoft.Azure.EventHubs.Processor`
1. Open the **Program.cs** file, and paste the following:
    ```cs
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using System;
    using System.Threading.Tasks;

    namespace EventHub.Consumer
    {
        public class Program
        {
            private const string EventHubConnectionString = "<event hub namespace connection string>";
            private const string EventHubName = "messaging_lab";
            private const string StorageConnectionString = "<storage account connection string>";
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
    ```
    Substitute you event hub namespace connection string, event hub name, storage account connection string, and container name.
1. Add a new class named `SimpleEventProcessor`, as follows:
    ```cs
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    namespace EventHub.Consumer
    {
        public class SimpleEventProcessor : IEventProcessor
        {
            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
                return Task.CompletedTask;
            }

            public Task OpenAsync(PartitionContext context)
            {
                Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
                return Task.CompletedTask;
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
                return Task.CompletedTask;
            }

            public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                foreach (var eventData in messages)
                {
                    var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");
                }

                return context.CheckpointAsync();
            }
        }
    }
    ```

Read through the code and ensure you understand the structure of how the application works.

## Run the applications
Start both the consumer and producer.
_Note:_ One can starting a second project in Visual Studio when one is already running. Right-click the project and select **Debug** > **Start new instance**.

Once several messages have been produced stop the producer (press **ENTER**).

## A comment on partitions and out-of-order messaging
Notice that the producer spreads its messages across paritions. When the consumer reads, the messages are retrieved in-order per partition, but not in-order across partitions.

Spreading across partitions allows for massive scale and resilience, but does come at the expense of in-order messaging. Applications that require massive scale must be designed too allow for our-of-order messaging.

In a further exercse in this lab we will see how to specify that messages be directed to a specific partition.

## Consumer state
Try stopping the consumer, and restarting it. Notice that the consumer has persisted its read state and so is aware of the last message read in the stream (previously processed messages are not re-read).

The consumer stores its read-state in the storage account. Open the Storage Account's **Storage Explorer**. Notice that the `checkpointcontainer` Blob container (as specified in the consumer Program.cs file) has been created. Under this is a folder for the consumer group _"$Default"_ (this is the default name of a consumer group, and is the value of `PartitionReceiver.DefaultConsumerGroupName` used in the consumer). Under the consumer group's folder is a file per partition (0, 1, and 2).

Try downloading one of these files and openning it in Notepad.

Now try deleting one of these files (stop the consumer before doing so), and then restart the consumer. Notice that the consumer will replay the messages it read earlier from the relevant partition.

## Specifying a partition
In the producer's `Program.cs` file, change the line:
```cs
await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
```
to
```cs
await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)), "MyPartition");
```
(i.e. add a "partitionKey" parameter to the SendAsync method).

The partition key is mapped to a specific partition by the Event Hub. All messages will now be sent to a single partition.
_Notice:_ The partition key does not specify the _physical_ partition, but does provide this mechanism to specify a _logic_ partition.

Restart the consumer and producer.  Notice that all messages have been received via a single partition.

## Going further
In this section we will create a new producer, but this time using the Kafka API.

1. Add a new .NET Core console project to your solution, `EventHub.Kafka.Producer`
1. Add the NuGet package `Confluent.Kafka`
1. Paste the code below, and replace your event hub's details below.
    ```cs
    using System;
    using System.Threading.Tasks;
    using Confluent.Kafka;

    namespace EventHub.Kafka.Producer
    {
        class Program
        {
            private const string EventHubNamespace = "<event hub namespace>";
            private const string EventHubAccessKey = "<event hub access key>";
            private const string EventHubName = "messaging_lab";

            public static async Task Main(string[] args)
            {
                string eventHubConnectionString = $"Endpoint=sb://{EventHubNamespace}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={EventHubAccessKey}";

                var config = new ProducerConfig {
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
    ```
    This code is a modified version of that found in **Confluent's .NET Client for Apache Kafka** examples.

Run this application and produce a message using the Kafka API.  Run your existing consumer and notice that the message can still be read using the Event Hubs SDK, even though it has been sent using Kafka.

Try writing a Kafka consumer, and use your Event Hub SDK producer to send messages to it. (See the "Basic Consumer Example" section in **Confluent's .NET Client for Apache Kafka**)

You can learn more about using Kafka with event hub:
+ [Quickstart: Data streaming with Event Hubs using the Kafka protocol](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-quickstart-kafka-enabled-event-hubs)
+ [Migrating to Azure Event Hubs for Apache Kafka Ecosystems](https://github.com/Azure/azure-event-hubs-for-kafka)
+ [Confluent's .NET Client for Apache Kafka](https://github.com/confluentinc/confluent-kafka-dotnet)

## Going even further
Try to get your Kafka producer and consumer working with the Apache Avro format serializers.

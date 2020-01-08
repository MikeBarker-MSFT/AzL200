
# Lab 1: Building a simple chat app using message bus

Work in pairs to create a chat app. Each partner in the pair will create a message bus queue to which you will send messages to your partner. Likewise you will use your partner's queue to receive message from them.

## Create a Rsource Group
1. Sign into the [Azure Portal](https://portal.azure.com/)
1. In the far-left menu select **Resource groups**
1. Click **+ Add**
    + _Name:_ `az{your_id}-messaging-rg`
    + _Region:_ `West Europe`
    + Click **Review + create**
    + Click **Create**


## Create a Message Bus namespace
A namespace is the logical container for queues and topics.

Within the portal:
1. From the left-hand menu click on **+ Create a resource**
1. Search for and select `Service Bus`
1. Click **Create**
1. In the Create namespace blade:
    + _Name_: `az{your_id}-messaging-servicebus`
    + _Pricing tier:_ `Basic`
    + _Resource group:_ `az{your_id}-messaging-rg`
    + _Location:_ `West Europe`
1. Click **Create**

The Service Bus namespace will take approximately 1.5 minutes to be created.

## Create a Queue
1. From the Service Bus Namespace blade create a new Queue. At the top of the main blade select **+ Queue**
    + _Name:_ `chat_queue`
    + Leave other options as default.
    + Click **Create**
1. Now retrieve a connection string for your service bus, and make note of it. You will need to paste this into the chat application.
    1. In the Left-hand menu select **Settings** > **Shared access policies**
    1. A Policy already exists which grants Manage, Send and Receive access. Click on the RootManageSharedAccessKey.
    1. From the Pop-up copy the **Primary Connection String**
        * This should start with `Endpoint=sb://az{your_id}-servicebus.servicebus.windows.net...`
    1. Also make a note of your queue's name, as you will need this too.

In this lab we are re-using a connection string which allows Manage, Send and Receive access rights. In practice one would only share a connection string which has the minimum rights required. Once you have completed this lab try to create a send-only and a receive-only connection string and use these in your chat application.

## Create a simple chat application

In Visual Studio:
1. Create a new .NET Console project.
    + File -> New Project
    + From the left menu **Visual C#** > **.NET Core**
    + Choose **Console App (.NET Core)**
    + Provide a name for your project, e.g. `ServiceBus.ChatApp`
    + Change the solution name to `ServiceBus`
    + Click **OK**
1. Add the Azure ServiceBus NuGet package
    + Right-click the project in the Solution Explorer
    + Manage NuGet Packages...
    + Change to the **Browse** tab at the tab of the window
    + Search for `Microsoft.Azure.ServiceBus`
    + Select the package and click Install
    + Click **OK** in the Preview Changes window, and accept the licenses.
1. Open the Program.cs file, and paste found at the end of this lab sheet.
    + Read through the code and ensure you understand the structure of how the application works.
1. Using the ServiceBus namespace connection string and queue you made note of previously, set your queue's connection string and name in the `SendServiceBusConnectionString` and `SendQueueName` variables.
1. Using your partner's connection string and queue name, set the `ReceiverServiceBusConnectionString` and `ReceiverQueueName`.
1. Build the application and run it.

You should now be able to type and send messages to your partner, and receive messages back from them.  Notice that messages sent whilst either you or your partner's application is not running will be received when the application is restarted.

## Going Further
As mentioned above, we have used a connection string which has Manage, Send and Receive permissions to our service bus namespace. Define two new shared access policies which will only allow one to Send, or to Receive. Use these in your, and your partner's applications to improve the security footprint.


## C# Chat Application

``` C#
using Microsoft.Azure.ServiceBus;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp
{
    class Program
    {
        const string SendServiceBusConnectionString = "<sender queue connection string>";
        const string SendQueueName = "<sender queue name>";
        static IQueueClient sendQueueClient;

        const string ReceiveServiceBusConnectionString = "<receiver queue connection string>";
        const string ReceiveQueueName = "<receiver queue name>";
        static IQueueClient receiveQueueClient;

        static void Main(string[] args)
        {
            InitializeReceiveQueue();

            MainLoopAsync().GetAwaiter().GetResult();
        }


        /****************************************************\
        |* SENDER                                           *|
        \****************************************************/

        /// <summary>
        /// Waits for user input and send the text via message bus.
        /// </summary>
        private static async Task MainLoopAsync()
        {
            sendQueueClient = new QueueClient(SendServiceBusConnectionString, SendQueueName);

            while (true)
            {
                var text = Console.ReadLine();

                if (text == "!q")
                    break;

                await SendMessageAsync(text);
            }

            await sendQueueClient.CloseAsync();
        }

        /// <summary>
        /// Sends a text message in encoded UTF to the sender queue.
        /// </summary>
        private static async Task SendMessageAsync(string text)
        {
            try
            {
                byte[] encodedText = Encoding.UTF8.GetBytes(text);

                Message message = new Message(encodedText);

                await sendQueueClient.SendAsync(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }


        /****************************************************\
        |* RECEIVER                                         *|
        \****************************************************/

        /// <summary>
        /// Create a receiver queue, and setup the message handler
        /// </summary>
        private static void InitializeReceiveQueue()
        {
            receiveQueueClient = new QueueClient(ReceiveServiceBusConnectionString, ReceiveQueueName);

            //Register the message and exception handlers
            receiveQueueClient.RegisterMessageHandler(ProcessMessagesAsync, ExceptionHandlerAsync);
        }

        /// <summary>
        /// Callback method, called when a message is received
        /// </summary>
        private static Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            byte[] encodedText = message.Body;

            string text = Encoding.UTF8.GetString(encodedText);

            Console.WriteLine();
            Console.WriteLine($">> {text}");
            Console.WriteLine();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Callback method, called when an exception is encountered during message processing
        /// </summary>
        private static Task ExceptionHandlerAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"{DateTime.Now} :: Exception: {exceptionReceivedEventArgs.Exception.Message}");

            return Task.CompletedTask;
        }
    }
}
```

using Microsoft.Azure.ServiceBus;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.ChatApp
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

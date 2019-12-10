using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ChatClient
{
    public class Producer: IHostedService
    {
        private readonly IConfiguration _config;

        public Producer(IConfiguration configuration)
        {
            _config = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string exchangeName = _config.GetSection("Exchange").GetValue<string>("Name");

            var factory = new ConnectionFactory
            {
                HostName = _config.GetSection("RabbitMQ").GetValue<string>("Host"), 
                UserName = _config.GetSection("RabbitMQ").GetValue<string>("UserName"),
                Password = _config.GetSection("RabbitMQ").GetValue<string>("Password"),
                Port = _config.GetSection("RabbitMQ").GetValue<int>("Port"), 
                VirtualHost = _config.GetSection("RabbitMQ").GetValue<string>("VirtualHost")
            };

            var user = Ask("Please enter your nick name: ");

            using (var connection = factory.CreateConnection())

            using (var channel = connection.CreateModel())
            {
                JoinChat(channel, user, exchangeName);

                while (!cancellationToken.IsCancellationRequested)
                {

                    var message = "";
                    ConsoleKeyInfo ch = Console.ReadKey(true);
                    while (ch.Key != ConsoleKey.Enter)
                    {
                        message += ch.KeyChar;
                        Console.Write(ch.KeyChar);
                        ch = Console.ReadKey(true);
                    }
                    ClearCurrentConsoleLine();

                    if (message != null && message.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Exit");
                        break;
                    }
                    else
                    {
                        PublishChat(channel, user, exchangeName, message);
                    }
                }

                LeaveChat(channel, user, exchangeName);
            }
        }

        private static string Ask(string prompt)
        {
            string result = null;
            while (String.IsNullOrEmpty(result))
            {
                Console.Write(prompt);
                result = Console.ReadLine();
            }
            return result;
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);

            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write(" ");
            }
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static void PublishChat(IModel channel, string user, string exchangeName, string message)
        {
            var msg = new Message { timestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds, nickname = user, message = message, type = "publish" };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Message>(msg));
            channel.BasicPublish(exchange: exchangeName,
                routingKey: "",
                basicProperties: null,
                body: body);
        }

        private static void JoinChat(IModel channel, string user, string exchangeName)
        {
            var msg = new Message { timestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds, nickname = user, type = "join" , message = "member has joined the chat" };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Message>(msg));
            channel.BasicPublish(exchange: exchangeName,
                routingKey: "",
                basicProperties: null,
                body: body);
        }

        private static void LeaveChat(IModel channel, string user, string exchangeName)
        {
            var msg = new Message { timestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds, nickname = user, type = "leave", message = "member has left the chat" };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Message>(msg));
            channel.BasicPublish(exchange: exchangeName,
                routingKey: "",
                basicProperties: null,
                body: body);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

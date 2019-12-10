using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatClient
{
    public class Consumer: BackgroundService
    {
        private readonly IConfiguration _config;

        public Consumer(IConfiguration configuration)
        {
            _config = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var queueName = channel.QueueDeclare("", false, false, true, null).QueueName;
                channel.QueueBind(queueName, exchangeName, "");
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += HandleMessage;

                await Task.Run(() => channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer));

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
        }

        private void HandleMessage(object model, BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var message = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(body));
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            if (message.type.Equals("join"))
            {
                Console.WriteLine("[{0}] {1} has joined the chat", dtDateTime.AddSeconds(message.timestamp), message.nickname);
            }
            else if (message.type.Equals("leave"))
            {
                Console.WriteLine("[{0}] {1} has left the chat", dtDateTime.AddSeconds(message.timestamp), message.nickname);
            }
            else if (message.type.Equals("publish"))
            {
                Console.WriteLine("[{0}] {1} > {2}", dtDateTime.AddSeconds(message.timestamp), message.nickname, message.message);
            }
        }
    }
}

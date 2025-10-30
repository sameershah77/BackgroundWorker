using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace BackgroundWorker
{
    public class AverageCalculationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string RequestQueue = "average_request_queue";
        private const string ResponseQueue = "average_response_queue";

        public AverageCalculationWorker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            var factory = new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(RequestQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(ResponseQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.BasicQos(0, 1, false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<RequestMessage>(json);
                if (msg == null) return;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DBContext>();

                var values = await db.Combinations
                    .Select(x => EF.Property<double?>(x, msg.ColumnName))
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToListAsync(stoppingToken);

                var average = values.Any() ? values.Average() : 0;
                Console.WriteLine($"✅ Average for {msg.ColumnName} = {average}");

                Console.WriteLine("Waiting...");
                Console.WriteLine("Wait completed");


                // Send response message
                var response = new
                {
                    msg.RequestId,
                    msg.ColumnName,
                    Average = average
                };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                _channel.BasicPublish("", ResponseQueue, null, body);
                _channel.BasicAck(ea.DeliveryTag, false);


            };

            _channel.BasicConsume(RequestQueue, false, consumer);
            return Task.CompletedTask;
        }

        private class RequestMessage
        {
            public string RequestId { get; set; } = string.Empty;
            public string ColumnName { get; set; } = string.Empty;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}

using BackgroundWorker;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace BackgroundWorker
{
    public class CombinationInsertWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string RequestQueue = "combination_insert_queue";
        private const string ResponseQueue = "combination_response_queue";

        public CombinationInsertWorker(IServiceScopeFactory scopeFactory)
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

            Console.WriteLine($"Varad Kumar");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var msg = JsonSerializer.Deserialize<AssetInsertMessage>(json);
                    if (msg == null)
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<DBContext>();

                    Console.WriteLine($"🧩 Generating combinations for: {msg.AssetName} ({msg.AssetId})");

                    var combos = CombinationHelper.GenerateCombinationData(msg.AssetName, msg.AssetId);

                    // ✅ Add all at once, instead of looping with Add()
                    await db.Combinations.AddRangeAsync(combos);
                    await db.SaveChangesAsync(stoppingToken);

                    var totalCount = await db.Combinations.CountAsync(stoppingToken);

                    Console.WriteLine($"✅ Inserted {combos.Count} combos. Total = {totalCount}");

                    // Send response (optional)
                    var response = new
                    {
                        msg.RequestId,
                        TotalCombinations = totalCount
                    };
                    var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                    _channel.BasicPublish("", ResponseQueue, null, responseBody);

                    // ✅ Acknowledge message only after success
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing message: {ex.Message}");

                    // ❌ Prevent RabbitMQ from endlessly redelivering
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                }
            };


            _channel.BasicConsume(RequestQueue, false, consumer);
            return Task.CompletedTask;
        }

        private class AssetInsertMessage
        {
            public string RequestId { get; set; } = string.Empty;
            public string AssetName { get; set; } = string.Empty;
            public int AssetId { get; set; }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}

using System.Text.Json;
using Confluent.Kafka;
using CosmoChess.Infrastructure.Kafka.Models;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace CosmoChess.Infrastructure.Kafka
{
    public class BotMoveProducer : IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<BotMoveProducer> _logger;
        private readonly string _topic;

        public BotMoveProducer(string bootstrapServers, string topic, ILogger<BotMoveProducer> logger)
        {
            _topic = topic;
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishBotMoveRequestAsync(KafkaBotMoveRequest request, CancellationToken cancellationToken = default)
        {
            using (LogContext.PushProperty("GameId", request.GameId))
            {
                try
                {
                    var json = JsonSerializer.Serialize(request);
                    var message = new Message<string, string>
                    {
                        Key = request.GameId.ToString(),
                        Value = json
                    };

                    var result = await _producer.ProduceAsync(_topic, message, cancellationToken);

                    _logger.LogInformation("Published bot move request for game {GameId} to partition {Partition} at offset {Offset}",
                        request.GameId, result.Partition.Value, result.Offset.Value);
                }
                catch (ProduceException<string, string> ex)
                {
                    _logger.LogError(ex, "Failed to publish bot move request for game {GameId}", request.GameId);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}

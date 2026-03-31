using System.Text.Json;
using CosmoChess.Infrastructure.Kafka;
using CosmoChess.Infrastructure.Kafka.Models;
using CosmoChess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CosmoChess.Infrastructure.Services
{
    public class OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        BotMoveProducer kafkaProducer,
        ILogger<OutboxProcessor> logger) : BackgroundService
    {
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);
        private const int BatchSize = 20;
        private const int MaxRetries = 5;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("OutboxProcessor started, polling every {Interval}s",
                _pollingInterval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processedCount = await ProcessBatchAsync(stoppingToken);

                    if (processedCount < BatchSize)
                    {
                        await Task.Delay(_pollingInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in OutboxProcessor polling loop");
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
            }

            logger.LogInformation("OutboxProcessor stopped");
        }

        private async Task<int> ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CosmoChessDbContext>();

            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
                .OrderBy(m => m.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await DispatchAsync(message, ct);
                    message.ProcessedAt = DateTime.UtcNow;

                    logger.LogInformation("Outbox message {MessageId} of type {Type} published",
                        message.Id, message.Type);
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message.Length > 1024 ? ex.Message[..1024] : ex.Message;

                    logger.LogWarning(ex,
                        "Failed to process outbox message {MessageId} (attempt {Attempt}/{MaxRetries})",
                        message.Id, message.RetryCount, MaxRetries);
                }
            }

            if (messages.Count > 0)
            {
                await dbContext.SaveChangesAsync(ct);
            }

            return messages.Count;
        }

        private async Task DispatchAsync(Domain.Entities.OutboxMessage message, CancellationToken ct)
        {
            switch (message.Type)
            {
                case "BotMoveRequest":
                    var request = JsonSerializer.Deserialize<KafkaBotMoveRequest>(message.Payload)
                        ?? throw new InvalidOperationException(
                            $"Failed to deserialize BotMoveRequest payload for message {message.Id}");
                    await kafkaProducer.PublishBotMoveRequestAsync(request, ct);
                    break;

                default:
                    logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                    break;
            }
        }
    }
}

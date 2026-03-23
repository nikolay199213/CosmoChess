using System.Threading.Channels;
using CosmoChess.Domain.Enums;
using CosmoChess.Infrastructure.Kafka;
using CosmoChess.Infrastructure.Kafka.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace CosmoChess.Infrastructure.Services
{
    public class BotMoveRequest
    {
        public Guid GameId { get; init; }
        public string CurrentFen { get; init; } = string.Empty;
        public BotDifficulty Difficulty { get; init; }
        public BotStyle Style { get; init; } = BotStyle.Balanced;
    }

    public class BotMoveResult
    {
        public Guid GameId { get; init; }
        public string Move { get; init; } = string.Empty;
        public string NewFen { get; init; } = string.Empty;
        public bool IsCheckmate { get; init; }
        public bool IsStalemate { get; init; }
        public bool IsDraw { get; init; }
        public int WhiteTimeRemainingSeconds { get; init; }
        public int BlackTimeRemainingSeconds { get; init; }
        public GameResult? GameResult { get; init; }
        public GameEndReason? EndReason { get; init; }
    }

    public interface IBotMoveService
    {
        void EnqueueBotMove(BotMoveRequest request);
    }

    public class BotMoveBackgroundService : BackgroundService, IBotMoveService
    {
        private readonly Channel<BotMoveRequest> _channel;
        private readonly BotMoveProducer _kafkaProducer;
        private readonly ILogger<BotMoveBackgroundService> _logger;

        public BotMoveBackgroundService(
            BotMoveProducer kafkaProducer,
            ILogger<BotMoveBackgroundService> logger)
        {
            _kafkaProducer = kafkaProducer;
            _logger = logger;
            _channel = Channel.CreateUnbounded<BotMoveRequest>();
        }

        public void EnqueueBotMove(BotMoveRequest request)
        {
            _channel.Writer.TryWrite(request);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BotMoveBackgroundService started (Kafka mode)");

            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                using (LogContext.PushProperty("GameId", request.GameId))
                {
                    try
                    {
                        await ProcessBotMoveAsync(request, stoppingToken);
                        _logger.LogInformation("Bot move request sent to Kafka for game {GameId}", request.GameId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending bot move request to Kafka for game {GameId}", request.GameId);
                    }
                }
            }
        }

        private async Task ProcessBotMoveAsync(BotMoveRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending bot move request to Kafka for game {GameId}", request.GameId);

            var kafkaRequest = new KafkaBotMoveRequest
            {
                GameId = request.GameId,
                Fen = request.CurrentFen,
                Difficulty = (int)request.Difficulty,
                Style = (int)request.Style,
                Timestamp = DateTime.UtcNow
            };

            await _kafkaProducer.PublishBotMoveRequestAsync(kafkaRequest, cancellationToken);
        }
    }
}

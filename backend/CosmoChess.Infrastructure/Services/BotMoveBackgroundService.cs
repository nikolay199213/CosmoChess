using System.Threading.Channels;
using CosmoChess.Domain.Enums;
using CosmoChess.Infrastructure.Kafka;
using CosmoChess.Infrastructure.Kafka.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        Task<BotMoveResult> RequestBotMoveAsync(BotMoveRequest request, CancellationToken cancellationToken = default);
        void EnqueueBotMove(BotMoveRequest request, Action<BotMoveResult> onComplete);
    }

    public class BotMoveBackgroundService : BackgroundService, IBotMoveService
    {
        private readonly Channel<(BotMoveRequest Request, Action<BotMoveResult> OnComplete)> _channel;
        private readonly BotMoveProducer _kafkaProducer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BotMoveBackgroundService> _logger;

        public BotMoveBackgroundService(
            BotMoveProducer kafkaProducer,
            IServiceScopeFactory scopeFactory,
            ILogger<BotMoveBackgroundService> logger)
        {
            _kafkaProducer = kafkaProducer;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _channel = Channel.CreateUnbounded<(BotMoveRequest, Action<BotMoveResult>)>();
        }

        public void EnqueueBotMove(BotMoveRequest request, Action<BotMoveResult> onComplete)
        {
            _channel.Writer.TryWrite((request, onComplete));
        }

        public async Task<BotMoveResult> RequestBotMoveAsync(BotMoveRequest request, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<BotMoveResult>();

            EnqueueBotMove(request, result => tcs.SetResult(result));

            return await tcs.Task;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BotMoveBackgroundService started (Kafka mode)");

            await foreach (var (request, onComplete) in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessBotMoveAsync(request, stoppingToken);

                    // Note: onComplete callback is not called anymore
                    // Results will be delivered via Kafka Consumer → SignalR
                    _logger.LogInformation("Bot move request sent to Kafka for game {GameId}", request.GameId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending bot move request to Kafka for game {GameId}", request.GameId);
                }
            }
        }

        private async Task ProcessBotMoveAsync(BotMoveRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending bot move request to Kafka for game {GameId}", request.GameId);

            // Convert domain request to Kafka message
            var kafkaRequest = new KafkaBotMoveRequest
            {
                GameId = request.GameId,
                Fen = request.CurrentFen,
                Difficulty = (int)request.Difficulty,
                Style = (int)request.Style,
                Timestamp = DateTime.UtcNow
            };

            // Send to Kafka (bot-service will process it)
            await _kafkaProducer.PublishBotMoveRequestAsync(kafkaRequest, cancellationToken);

            _logger.LogInformation("Bot move request sent to Kafka for game {GameId}", request.GameId);
        }
    }
}
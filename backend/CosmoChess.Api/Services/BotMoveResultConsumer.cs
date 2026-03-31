using System.Text.Json;
using Confluent.Kafka;
using CosmoChess.Api.Hubs;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Infrastructure.Kafka.Models;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace CosmoChess.Api.Services
{
    public class BotMoveResultConsumer : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BotMoveResultConsumer> _logger;
        private readonly string _topic;

        public BotMoveResultConsumer(
            string bootstrapServers,
            string groupId,
            string topic,
            IHubContext<GameHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger<BotMoveResultConsumer> logger)
        {
            _topic = topic;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _logger = logger;

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BotMoveResultConsumer started. Subscribed to topic: {Topic}", _topic);

            _consumer.Subscribe(_topic);

            // Yield to allow the host to finish starting (Kestrel, etc.)
            await Task.Yield();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        _logger.LogInformation("Received bot move result. Key: {Key}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Message.Key, consumeResult.Partition.Value, consumeResult.Offset.Value);

                        var result = JsonSerializer.Deserialize<KafkaBotMoveResult>(consumeResult.Message.Value);
                        if (result == null)
                        {
                            _logger.LogWarning("Failed to deserialize bot move result");
                            _consumer.Commit(consumeResult);
                            continue;
                        }

                        using (LogContext.PushProperty("GameId", result.GameId))
                        {
                            await ProcessBotMoveResultAsync(result, stoppingToken);
                        }

                        _consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing bot move result");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BotMoveResultConsumer stopping");
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("BotMoveResultConsumer stopped");
            }
        }

        private async Task ProcessBotMoveResultAsync(KafkaBotMoveResult result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing bot move result for game {GameId}: move {Move}", result.GameId, result.Move);

            try
            {
                // Save move to database
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

                var game = await repository.GetById(result.GameId, cancellationToken);

                // Idempotency check: if the game's current FEN doesn't match the FEN
                // the bot was asked to analyze, this is a stale/duplicate result — skip it.
                if (!string.IsNullOrEmpty(result.RequestFen) && game.CurrentFen != result.RequestFen)
                {
                    _logger.LogWarning(
                        "Stale bot move result for game {GameId}: expected FEN {Expected}, actual FEN {Actual}. Skipping.",
                        result.GameId, result.RequestFen, game.CurrentFen);
                    return;
                }

                game.MakeMove(Game.BotPlayerId, result.Move, result.NewFen, result.IsCheckmate, result.IsStalemate, result.IsDraw);
                await repository.Update(game, cancellationToken);

                _logger.LogInformation("Bot move saved to database for game {GameId}", result.GameId);

                // Send SignalR notification to game room
                var gameGroupName = $"game_{result.GameId}";
                await _hubContext.Clients.Group(gameGroupName).SendAsync(
                    "MoveReceived",
                    new
                    {
                        gameId = result.GameId,
                        userId = Game.BotPlayerId,
                        move = result.Move,
                        newFen = result.NewFen,
                        whiteTimeRemainingSeconds = game.WhiteTimeRemainingSeconds,
                        blackTimeRemainingSeconds = game.BlackTimeRemainingSeconds
                    },
                    cancellationToken);

                _logger.LogInformation("SignalR notification sent for bot move in game {GameId}", result.GameId);

                // Notify about game state change if game ended
                if (result.IsCheckmate || result.IsStalemate || result.IsDraw)
                {
                    await _hubContext.Clients.Group(gameGroupName).SendAsync(
                        "GameStateChanged",
                        new
                        {
                            gameId = result.GameId,
                            gameResult = (int)game.GameResult,
                            endReason = (int)game.EndReason
                        },
                        cancellationToken);

                    _logger.LogInformation("Game ended notification sent for game {GameId}", result.GameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bot move result for game {GameId}", result.GameId);
                throw;
            }
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}

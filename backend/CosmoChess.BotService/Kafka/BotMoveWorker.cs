using System.Text.Json;
using Chess;
using Confluent.Kafka;
using CosmoChess.BotService.Configuration;
using CosmoChess.BotService.Models;
using CosmoChess.BotService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CosmoChess.BotService.Kafka
{
    public class BotMoveWorker : BackgroundService
    {
        private readonly BotConfiguration _configuration;
        private readonly BotMoveCalculator _botCalculator;
        private readonly ILogger<BotMoveWorker> _logger;

        public BotMoveWorker(
            BotConfiguration configuration,
            BotMoveCalculator botCalculator,
            ILogger<BotMoveWorker> logger)
        {
            _configuration = configuration;
            _botCalculator = botCalculator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BotMoveWorker started. Kafka: {Kafka}, Group: {Group}",
                _configuration.KafkaBootstrapServers, _configuration.KafkaGroupId);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration.KafkaBootstrapServers,
                GroupId = _configuration.KafkaGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration.KafkaBootstrapServers
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            consumer.Subscribe(_configuration.RequestTopic);
            _logger.LogInformation("Subscribed to topic: {Topic}", _configuration.RequestTopic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        _logger.LogInformation("Received message. Key: {Key}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Message.Key, consumeResult.Partition.Value, consumeResult.Offset.Value);

                        var request = JsonSerializer.Deserialize<BotMoveRequest>(consumeResult.Message.Value);
                        if (request == null)
                        {
                            _logger.LogWarning("Failed to deserialize message");
                            consumer.Commit(consumeResult);
                            continue;
                        }

                        // Process bot move
                        var result = await ProcessBotMoveAsync(request, stoppingToken);

                        // Produce result
                        var resultJson = JsonSerializer.Serialize(result);
                        var message = new Message<string, string>
                        {
                            Key = result.GameId.ToString(),
                            Value = resultJson
                        };

                        await producer.ProduceAsync(_configuration.ResultTopic, message, stoppingToken);
                        _logger.LogInformation("Published result for game {GameId}, move: {Move}",
                            result.GameId, result.Move);

                        // Commit offset
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing bot move");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BotMoveWorker stopping");
            }
            finally
            {
                consumer.Close();
                _logger.LogInformation("BotMoveWorker stopped");
            }
        }

        private async Task<BotMoveResult> ProcessBotMoveAsync(BotMoveRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing bot move for game {GameId}, difficulty: {Difficulty}, style: {Style}",
                request.GameId, request.Difficulty, request.Style);

            // Add thinking delay for realism
            var difficulty = (BotDifficulty)request.Difficulty;
            var style = (BotStyle)request.Style;
            var delay = _botCalculator.GetThinkingDelayMs(difficulty);
            await Task.Delay(delay, cancellationToken);

            // Get best move from bot calculator
            var uciMove = await _botCalculator.GetBotMoveAsync(request.Fen, difficulty, style, cancellationToken);

            // Apply move using Gera.Chess to get new FEN and check game state
            var board = ChessBoard.LoadFromFen(request.Fen);

            var from = uciMove[..2];
            var to = uciMove[2..4];

            if (uciMove.Length > 4)
            {
                // Promotion move
                board.Move(new Move(from, to));
            }
            else
            {
                board.Move(new Move(from, to));
            }

            var newFen = board.ToFen();

            // Check game end conditions
            var isCheckmate = board.IsEndGame && board.EndGame?.EndgameType == EndgameType.Checkmate;
            var isStalemate = board.IsEndGame && board.EndGame?.EndgameType == EndgameType.Stalemate;
            var isDraw = board.IsEndGame && (
                board.EndGame?.EndgameType == EndgameType.InsufficientMaterial ||
                board.EndGame?.EndgameType == EndgameType.FiftyMoveRule ||
                board.EndGame?.EndgameType == EndgameType.Repetition ||
                board.EndGame?.EndgameType == EndgameType.DrawDeclared
            );

            _logger.LogInformation("Bot made move {Move} in game {GameId}. Checkmate: {Checkmate}, Stalemate: {Stalemate}, Draw: {Draw}",
                uciMove, request.GameId, isCheckmate, isStalemate, isDraw);

            return new BotMoveResult
            {
                GameId = request.GameId,
                Move = uciMove,
                NewFen = newFen,
                IsCheckmate = isCheckmate,
                IsStalemate = isStalemate,
                IsDraw = isDraw,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}

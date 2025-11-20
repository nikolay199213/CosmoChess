using System.Threading.Channels;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Domain.Interface.Services;
using Chess;
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
        private readonly IBotService _botService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BotMoveBackgroundService> _logger;

        public BotMoveBackgroundService(
            IBotService botService,
            IServiceScopeFactory scopeFactory,
            ILogger<BotMoveBackgroundService> logger)
        {
            _botService = botService;
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
            _logger.LogInformation("BotMoveBackgroundService started");

            await foreach (var (request, onComplete) in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var result = await ProcessBotMoveAsync(request, stoppingToken);
                    onComplete(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bot move for game {GameId}", request.GameId);
                }
            }
        }

        private async Task<BotMoveResult> ProcessBotMoveAsync(BotMoveRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing bot move for game {GameId}", request.GameId);

            // Add thinking delay for realism
            var delay = _botService.GetThinkingDelayMs(request.Difficulty);
            await Task.Delay(delay, cancellationToken);

            // Get best move from engine (UCI format like "e2e4")
            var uciMove = await _botService.GetBotMoveAsync(request.CurrentFen, request.Difficulty, cancellationToken);

            // Use Gera.Chess to apply move, check game state and get new FEN
            var board = ChessBoard.LoadFromFen(request.CurrentFen);

            // Parse UCI move (e2e4 -> "e2" to "e4", e7e8q -> "e7" to "e8" promote to 'q')
            var from = uciMove[..2];
            var to = uciMove[2..4];

            if (uciMove.Length > 4)
            {
                // Promotion move
                var promotionPiece = uciMove[4];
                board.Move(new Move(from, to));
            }
            else
            {
                board.Move(new Move(from, to));
            }

            // Get new FEN using ToFen() method
            var newFen = board.ToFen();

            // Check game end conditions using EndGame property
            var isCheckmate = board.IsEndGame && board.EndGame?.EndgameType == EndgameType.Checkmate;
            var isStalemate = board.IsEndGame && board.EndGame?.EndgameType == EndgameType.Stalemate;
            var isDraw = board.IsEndGame && (
                board.EndGame?.EndgameType == EndgameType.InsufficientMaterial ||
                board.EndGame?.EndgameType == EndgameType.FiftyMoveRule ||
                board.EndGame?.EndgameType == EndgameType.Repetition ||
                board.EndGame?.EndgameType == EndgameType.DrawDeclared
            );

            // Use UCI notation for move (client will convert if needed)
            var move = uciMove;

            // Save move to database
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

            var game = await repository.GetById(request.GameId, cancellationToken);
            game.MakeMove(Game.BotPlayerId, move, newFen, isCheckmate, isStalemate, isDraw);
            await repository.Update(game, cancellationToken);

            _logger.LogInformation("Bot made move {Move} in game {GameId}", move, request.GameId);

            return new BotMoveResult
            {
                GameId = request.GameId,
                Move = move,
                NewFen = newFen,
                IsCheckmate = isCheckmate,
                IsStalemate = isStalemate,
                IsDraw = isDraw,
                WhiteTimeRemainingSeconds = game.WhiteTimeRemainingSeconds,
                BlackTimeRemainingSeconds = game.BlackTimeRemainingSeconds,
                GameResult = isCheckmate || isStalemate || isDraw ? game.GameResult : null,
                EndReason = isCheckmate || isStalemate || isDraw ? game.EndReason : null
            };
        }
    }
}
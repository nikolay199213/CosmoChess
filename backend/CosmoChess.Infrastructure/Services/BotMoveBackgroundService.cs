using System.Threading.Channels;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Domain.Interface.Services;
using Gera.Chess;
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

            // Parse UCI move
            var from = uciMove[..2];
            var to = uciMove[2..4];
            var promotion = uciMove.Length > 4 ? uciMove[4].ToString() : null;

            // Use Gera.Chess to apply move and get new FEN
            var board = Board.FromFEN(request.CurrentFen);

            var fromSquare = ParseSquare(from);
            var toSquare = ParseSquare(to);

            var move = new Move(fromSquare, toSquare);
            if (promotion != null)
            {
                var promoType = promotion.ToLower() switch
                {
                    "q" => PieceType.Queen,
                    "r" => PieceType.Rook,
                    "b" => PieceType.Bishop,
                    "n" => PieceType.Knight,
                    _ => PieceType.Queen
                };
                move = new Move(fromSquare, toSquare, promoType);
            }

            board.MakeMove(move);
            var newFen = board.ToFEN();

            // Check game end conditions
            var isCheckmate = board.IsCheckmate();
            var isStalemate = board.IsStalemate();
            var isDraw = board.IsDraw() && !isStalemate;

            // Get SAN notation for the move
            var sanMove = uciMove; // For now use UCI, can convert to SAN later

            // Save move to database
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

            var game = await repository.GetById(request.GameId, cancellationToken);
            game.MakeMove(Game.BotPlayerId, sanMove, newFen, isCheckmate, isStalemate, isDraw);
            await repository.Update(game, cancellationToken);

            _logger.LogInformation("Bot made move {Move} in game {GameId}", sanMove, request.GameId);

            return new BotMoveResult
            {
                GameId = request.GameId,
                Move = sanMove,
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

        private static Square ParseSquare(string notation)
        {
            var file = notation[0] - 'a';
            var rank = notation[1] - '1';
            return (Square)(rank * 8 + file);
        }
    }
}
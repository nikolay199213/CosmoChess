using CosmoChess.Domain.Enums;
using CosmoChess.Domain.ValueObject;

namespace CosmoChess.Domain.Entities
{
    public class Game
    {
        public const string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime StartedAt { get; private set; } = DateTime.UtcNow;

        private readonly List<GameMove> _moves = [];
        public IReadOnlyCollection<GameMove> Moves => _moves.AsReadOnly();
        public GameType GameType { get; private set; }
        public GameResult GameResult { get; private set; } = GameResult.WaitJoin;
        public GameEndReason EndReason { get; private set; } = GameEndReason.None;
        public string CurrentFen { get; private set; } = InitialFen;

        public Game(Guid whitePlayerId)
        {
            WhitePlayerId = whitePlayerId;
        }

        public void Join(Guid blackPlayerId)
        {
            BlackPlayerId = blackPlayerId;
            GameResult = GameResult.InProgress;
        }

        public Guid WhitePlayerId { get; private set; }
        public Guid BlackPlayerId { get; private set; }

        public void MakeMove(Guid userId, string move, string newFen)
        {
            // Validate that it's the player's turn
            var isWhiteTurn = CurrentFen.Contains(" w ");
            var isWhitePlayer = userId == WhitePlayerId;
            var isBlackPlayer = userId == BlackPlayerId;

            if (isWhiteTurn && !isWhitePlayer)
            {
                throw new InvalidOperationException("It's white's turn");
            }

            if (!isWhiteTurn && !isBlackPlayer)
            {
                throw new InvalidOperationException("It's black's turn");
            }

            var gameMove = new GameMove(Id, userId, move, newFen, DateTime.UtcNow);
            _moves.Add(gameMove);
            CurrentFen = newFen;
        }
    }
}

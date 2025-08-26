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
        }

        public Guid WhitePlayerId { get; private set; }
        public Guid BlackPlayerId { get; private set; }

        public void MakeMove(Guid userId, string move, string newFen)
        {

            var gameMove = new GameMove(Id, userId, move, newFen, DateTime.Now);
            _moves.Add(gameMove);
        }
    }
}

namespace CosmoChess.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
        private readonly List<GameMove> _moves = [];

        public Game(Guid whitePlayerId, Guid blackPlayerId)
        {
            WhitePlayerId = whitePlayerId;
            BlackPlayerId = blackPlayerId;
        }

        public Guid WhitePlayerId { get; private set; }
        public Guid BlackPlayerId { get; private set; }

        public void MakeMove(GameMove move)
        {
            _moves.Add(move);
        }
    }
}

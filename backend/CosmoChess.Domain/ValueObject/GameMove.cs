namespace CosmoChess.Domain.ValueObject
{
    public class GameMove
    {
        public Guid GameId { get; set; }
        public string Move { get; private set; } = string.Empty;
        public string FenAfterMove { get; private set; } = null!;
        public Guid UserId { get; private set; }
        public DateTime MadeAt { get; private set; }
        public GameMove(Guid gameId, Guid userId, string move, string fenAfterMove, DateTime madeAt)
        {
            GameId = gameId;
            UserId = userId;
            Move = move;
            FenAfterMove = fenAfterMove;
            MadeAt = madeAt;
        }
    }
}

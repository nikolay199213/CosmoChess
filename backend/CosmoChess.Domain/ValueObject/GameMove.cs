namespace CosmoChess.Domain.ValueObject
{
    public class GameMove
    {
        public Guid GameId { get; set; }
        public string Move { get; private set; } = string.Empty;
        public string FenAfterMove { get; private set; } = null!;
        public DateTime MadeAt { get; private set; }
    }
}

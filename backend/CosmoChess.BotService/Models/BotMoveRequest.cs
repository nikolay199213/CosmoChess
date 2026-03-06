namespace CosmoChess.BotService.Models
{
    public class BotMoveRequest
    {
        public Guid GameId { get; set; }
        public string Fen { get; set; } = string.Empty;
        public int Difficulty { get; set; }  // 1-6 (Beginner to Master)
        public int Style { get; set; }       // 0=Balanced, 1=Aggressive, 2=Solid
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

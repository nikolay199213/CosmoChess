namespace CosmoChess.BotService.Models
{
    public class BotMoveResult
    {
        public Guid GameId { get; set; }
        public string Move { get; set; } = string.Empty;  // UCI format
        public string NewFen { get; set; } = string.Empty;
        public string RequestFen { get; set; } = string.Empty; // FEN before bot move (for idempotency)
        public bool IsCheckmate { get; set; }
        public bool IsStalemate { get; set; }
        public bool IsDraw { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

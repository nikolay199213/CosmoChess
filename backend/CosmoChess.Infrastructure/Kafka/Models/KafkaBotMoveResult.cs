namespace CosmoChess.Infrastructure.Kafka.Models
{
    public class KafkaBotMoveResult
    {
        public Guid GameId { get; set; }
        public string Move { get; set; } = string.Empty;
        public string NewFen { get; set; } = string.Empty;
        public bool IsCheckmate { get; set; }
        public bool IsStalemate { get; set; }
        public bool IsDraw { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

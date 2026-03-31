namespace CosmoChess.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Type { get; init; } = string.Empty;       // "BotMoveRequest"
        public string Payload { get; init; } = string.Empty;    // JSON
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public int RetryCount { get; set; }
        public string? Error { get; set; }
    }
}

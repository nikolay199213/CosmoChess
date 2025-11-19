using CosmoChess.Domain.Enums;
using CosmoChess.Domain.ValueObject;

namespace CosmoChess.Application.DTOs
{
    public class GameWithPlayersDto
    {
        public Guid Id { get; set; }
        public DateTime StartedAt { get; set; }
        public List<GameMoveDto> Moves { get; set; } = new();
        public GameType GameType { get; set; }
        public GameResult GameResult { get; set; }
        public GameEndReason EndReason { get; set; }
        public string CurrentFen { get; set; } = string.Empty;
        public TimeControl TimeControl { get; set; }
        public int WhiteTimeRemainingSeconds { get; set; }
        public int BlackTimeRemainingSeconds { get; set; }
        public DateTime? LastMoveTime { get; set; }

        public Guid WhitePlayerId { get; set; }
        public Guid BlackPlayerId { get; set; }

        // Player usernames
        public string? WhitePlayerUsername { get; set; }
        public string? BlackPlayerUsername { get; set; }
    }

    public class GameMoveDto
    {
        public Guid GameId { get; set; }
        public Guid UserId { get; set; }
        public string Move { get; set; } = string.Empty;
        public string ResultFen { get; set; } = string.Empty;
        public DateTime MadeAt { get; set; }
    }
}

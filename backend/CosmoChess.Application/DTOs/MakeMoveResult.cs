using CosmoChess.Domain.Enums;

namespace CosmoChess.Application.DTOs
{
    public class MakeMoveResult
    {
        public Guid GameId { get; init; }
        public GameType GameType { get; init; }
        public GameResult GameResult { get; init; }
        public GameEndReason EndReason { get; init; }
        public int WhiteTimeRemainingSeconds { get; init; }
        public int BlackTimeRemainingSeconds { get; init; }
        public string CurrentFen { get; init; } = string.Empty;
        public bool IsBotTurn { get; init; }
        public BotDifficulty? BotDifficulty { get; init; }
        public BotStyle? BotStyle { get; init; }

        public bool GameEnded => (int)GameResult >= 2;
    }
}

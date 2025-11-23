using CosmoChess.Domain.Enums;

namespace CosmoChess.Domain.Interface.Services
{
    public interface IBotService
    {
        Task<string> GetBotMoveAsync(string fen, BotDifficulty difficulty, BotStyle style = BotStyle.Balanced, CancellationToken cancellationToken = default);
        int GetThinkingDelayMs(BotDifficulty difficulty);
    }
}
using CosmoChess.Domain.Enums;

namespace CosmoChess.Domain.Interface.Services
{
    public interface IBotService
    {
        Task<string> GetBotMoveAsync(string fen, BotDifficulty difficulty, CancellationToken cancellationToken = default);
        int GetThinkingDelayMs(BotDifficulty difficulty);
    }
}
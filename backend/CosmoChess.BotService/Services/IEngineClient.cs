using CosmoChess.BotService.Models;

namespace CosmoChess.BotService.Services
{
    public interface IEngineClient
    {
        Task<AnalysisResult> AnalyzeMultiPvAsync(string fen, int depth, int multiPv, CancellationToken cancellationToken = default);
    }
}

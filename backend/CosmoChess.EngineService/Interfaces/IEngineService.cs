using CosmoChess.EngineService.Models;

namespace CosmoChess.EngineService.Interfaces
{
    public interface IEngineService
    {
        Task<string> AnalyzeAsync(string fen, int depth, CancellationToken cancellationToken);
        Task<AnalysisResult> AnalyzeMultiPvAsync(string fen, int depth, int multiPv, CancellationToken cancellationToken);
    }
}

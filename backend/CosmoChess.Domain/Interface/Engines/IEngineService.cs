using CosmoChess.Domain.ValueObject;

namespace CosmoChess.Domain.Interface.Engines
{
    public interface IEngineService
    {
        public Task<string> AnalyzeAsync(string fen, int depth, CancellationToken cancellationToken);
        public Task<AnalysisResult> AnalyzeMultiPvAsync(string fen, int depth, int multiPv, CancellationToken cancellationToken);
    }
}
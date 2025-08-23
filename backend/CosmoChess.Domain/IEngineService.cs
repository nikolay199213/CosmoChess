namespace CosmoChess.Domain
{
    public interface IEngineService
    {
        public Task<string> AnalyzeAsync(string fen, int depth, CancellationToken cancellationToken);
    }
}
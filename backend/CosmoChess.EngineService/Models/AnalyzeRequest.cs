namespace CosmoChess.EngineService.Models
{
    public record AnalyzeRequest(
        string Fen,
        int Depth
    );

    public record AnalyzeMultiPvRequest(
        string Fen,
        int Depth,
        int MultiPv
    );

    public record AnalyzeResponse(
        string BestMove
    );
}

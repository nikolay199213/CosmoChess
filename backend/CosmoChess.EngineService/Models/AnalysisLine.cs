namespace CosmoChess.EngineService.Models
{
    public record AnalysisLine(
        int Rank,           // 1, 2, 3 - line ranking
        string Move,        // Best move in UCI format (e2e4)
        string MoveSan,     // Best move in SAN format (e4)
        int Score,          // Evaluation in centipawns
        bool IsMate,        // Is checkmate
        int? MateIn,        // Mate in N moves
        string Pv           // Principal Variation - line of moves
    );

    public record AnalysisResult(
        List<AnalysisLine> Lines,
        int Depth
    );
}

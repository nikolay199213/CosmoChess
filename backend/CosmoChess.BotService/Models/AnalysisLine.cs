namespace CosmoChess.BotService.Models
{
    public record AnalysisLine(
        int Rank,
        string Move,
        string MoveSan,
        int Score,
        bool IsMate,
        int? MateIn,
        string Pv
    );

    public record AnalysisResult(
        List<AnalysisLine> Lines,
        int Depth
    );
}

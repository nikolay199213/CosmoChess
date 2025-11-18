namespace CosmoChess.Domain.ValueObject
{
    public record AnalysisLine(
        int Rank,           // 1, 2, 3 - номер линии
        string Move,        // Лучший ход в UCI формате (e2e4)
        string MoveSan,     // Лучший ход в SAN формате (e4)
        int Score,          // Оценка в сантипешках (centipawns)
        bool IsMate,        // Если мат
        int? MateIn,        // Мат в N ходов
        string Pv           // Principal Variation - линия ходов
    );

    public record AnalysisResult(
        List<AnalysisLine> Lines,
        int Depth
    );
}

using CosmoChess.BotService.Models;

namespace CosmoChess.BotService.Services
{
    public class BotMoveCalculator
    {
        private readonly IEngineClient _engineClient;
        private readonly ILogger<BotMoveCalculator> _logger;
        private readonly Random _random = new();

        public BotMoveCalculator(IEngineClient engineClient, ILogger<BotMoveCalculator> logger)
        {
            _engineClient = engineClient;
            _logger = logger;
        }

        public async Task<string> GetBotMoveAsync(string fen, BotDifficulty difficulty, BotStyle style, CancellationToken cancellationToken = default)
        {
            var depth = GetDepthForDifficulty(difficulty);
            var multiPv = GetMultiPvForDifficulty(difficulty);

            _logger.LogInformation("Bot calculating move for difficulty {Difficulty}, style {Style} at depth {Depth}, multiPv {MultiPv}",
                difficulty, style, depth, multiPv);

            try
            {
                var analysis = await _engineClient.AnalyzeMultiPvAsync(fen, depth, multiPv, cancellationToken);

                if (analysis.Lines == null || analysis.Lines.Count == 0)
                {
                    throw new InvalidOperationException("Engine returned no analysis lines");
                }

                // Filter out obvious blunders
                var filteredLines = FilterObviousBlunders(analysis.Lines, difficulty);

                // Check if we can capture an undefended piece
                var capturingMove = TryGetCapturingMove(filteredLines, difficulty);
                if (capturingMove != null)
                {
                    _logger.LogInformation("Bot found capturing move: {Move}", capturingMove);
                    return capturingMove;
                }

                // Choose move based on difficulty
                var selectedMove = SelectMoveByDifficulty(filteredLines, difficulty);

                // Apply style preference
                selectedMove = ApplyStylePreference(filteredLines, selectedMove, style, difficulty);

                _logger.LogInformation("Bot calculated {LineCount} moves, selected: {Move} (style: {Style})",
                    analysis.Lines.Count, selectedMove, style);

                return selectedMove;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating bot move");
                throw;
            }
        }

        public int GetThinkingDelayMs(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Beginner => _random.Next(500, 1500),
                BotDifficulty.Easy => _random.Next(800, 2000),
                BotDifficulty.Medium => _random.Next(1000, 2500),
                BotDifficulty.Hard => _random.Next(1500, 3000),
                BotDifficulty.Expert => _random.Next(2000, 4000),
                BotDifficulty.Master => _random.Next(2500, 5000),
                _ => _random.Next(1000, 2000)
            };
        }

        private string SelectMoveByDifficulty(IReadOnlyList<AnalysisLine> lines, BotDifficulty difficulty)
        {
            var rand = _random.Next(100);

            return difficulty switch
            {
                BotDifficulty.Beginner => rand switch
                {
                    < 60 => GetWorstMove(lines),
                    < 90 => GetMediumMove(lines),
                    _ => lines[0].Move
                },
                BotDifficulty.Easy => rand switch
                {
                    < 40 => GetMediumMove(lines),
                    < 80 => GetGoodMove(lines),
                    _ => lines[0].Move
                },
                BotDifficulty.Medium => rand switch
                {
                    < 20 => GetMediumMove(lines),
                    < 70 => GetGoodMove(lines),
                    _ => lines[0].Move
                },
                BotDifficulty.Hard => rand switch
                {
                    < 10 => GetGoodMove(lines),
                    _ => lines[0].Move
                },
                _ => lines[0].Move
            };
        }

        private string GetWorstMove(IReadOnlyList<AnalysisLine> lines)
        {
            var worstCount = Math.Max(1, lines.Count / 5);
            var startIndex = Math.Max(0, lines.Count - worstCount);
            var randomIndex = _random.Next(startIndex, lines.Count);
            return lines[randomIndex].Move;
        }

        private string GetMediumMove(IReadOnlyList<AnalysisLine> lines)
        {
            var middleStart = lines.Count / 3;
            var middleEnd = (lines.Count * 2) / 3;
            if (middleStart >= middleEnd) return lines[Math.Min(1, lines.Count - 1)].Move;
            var randomIndex = _random.Next(middleStart, middleEnd);
            return lines[randomIndex].Move;
        }

        private string GetGoodMove(IReadOnlyList<AnalysisLine> lines)
        {
            var goodCount = Math.Max(1, lines.Count / 3);
            var randomIndex = _random.Next(0, Math.Min(goodCount, lines.Count));
            return lines[randomIndex].Move;
        }

        private static int GetMultiPvForDifficulty(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Beginner => 10,
                BotDifficulty.Easy => 8,
                BotDifficulty.Medium => 5,
                BotDifficulty.Hard => 3,
                _ => 1
            };
        }

        private static int GetDepthForDifficulty(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Beginner => 1,
                BotDifficulty.Easy => 3,
                BotDifficulty.Medium => 6,
                BotDifficulty.Hard => 10,
                BotDifficulty.Expert => 15,
                BotDifficulty.Master => 20,
                _ => 10
            };
        }

        private List<AnalysisLine> FilterObviousBlunders(IReadOnlyList<AnalysisLine> lines, BotDifficulty difficulty)
        {
            if (difficulty == BotDifficulty.Beginner)
                return lines.ToList();

            var blunderThreshold = difficulty switch
            {
                BotDifficulty.Easy => -300,
                BotDifficulty.Medium => -200,
                BotDifficulty.Hard => -150,
                _ => -100
            };

            var bestScore = lines[0].Score;
            var filteredLines = lines
                .Where(line => line.Score - bestScore >= blunderThreshold)
                .ToList();

            if (filteredLines.Count == 0)
            {
                _logger.LogWarning("All moves filtered as blunders, keeping top 3 moves");
                return lines.Take(3).ToList();
            }

            _logger.LogInformation("Filtered {Original} moves to {Filtered} moves",
                lines.Count, filteredLines.Count);

            return filteredLines;
        }

        private string? TryGetCapturingMove(IReadOnlyList<AnalysisLine> lines, BotDifficulty difficulty)
        {
            var capturingMoves = lines
                .Where(l => l.Score > 200)
                .ToList();

            if (capturingMoves.Count == 0)
                return null;

            var captureChance = difficulty switch
            {
                BotDifficulty.Beginner => 50,
                BotDifficulty.Easy => 70,
                BotDifficulty.Medium => 85,
                BotDifficulty.Hard => 95,
                _ => 98
            };

            if (_random.Next(100) >= captureChance)
                return null;

            var topCaptures = capturingMoves.Take(Math.Max(1, capturingMoves.Count / 2)).ToList();
            var randomIndex = _random.Next(topCaptures.Count);
            return topCaptures[randomIndex].Move;
        }

        private string ApplyStylePreference(IReadOnlyList<AnalysisLine> lines, string selectedMove, BotStyle style, BotDifficulty difficulty)
        {
            if (style == BotStyle.Balanced || lines.Count <= 1)
                return selectedMove;

            var styleStrength = difficulty switch
            {
                BotDifficulty.Beginner => 70,
                BotDifficulty.Easy => 60,
                BotDifficulty.Medium => 50,
                BotDifficulty.Hard => 30,
                _ => 15
            };

            if (_random.Next(100) >= styleStrength)
                return selectedMove;

            return style switch
            {
                BotStyle.Aggressive => SelectAggressiveMove(lines, selectedMove),
                BotStyle.Solid => SelectSolidMove(lines, selectedMove),
                _ => selectedMove
            };
        }

        private string SelectAggressiveMove(IReadOnlyList<AnalysisLine> lines, string fallback)
        {
            var aggressiveMoves = lines
                .Where(l => IsAggressiveMove(l))
                .Take(Math.Max(1, lines.Count / 2))
                .ToList();

            if (aggressiveMoves.Count == 0)
                return fallback;

            var randomIndex = _random.Next(aggressiveMoves.Count);
            return aggressiveMoves[randomIndex].Move;
        }

        private string SelectSolidMove(IReadOnlyList<AnalysisLine> lines, string fallback)
        {
            var solidMoves = lines
                .Where(l => !IsAggressiveMove(l))
                .Take(Math.Max(1, lines.Count / 2))
                .ToList();

            if (solidMoves.Count == 0)
                return fallback;

            var randomIndex = _random.Next(Math.Min(3, solidMoves.Count));
            return solidMoves[randomIndex].Move;
        }

        private static bool IsAggressiveMove(AnalysisLine line)
        {
            if (string.IsNullOrEmpty(line.Pv))
                return false;

            var firstMove = line.Pv.Split(' ').FirstOrDefault() ?? "";
            var isSharpScore = Math.Abs(line.Score) > 100;
            var looksLikeCapture = firstMove.Length >= 4 && firstMove[0] != firstMove[2];

            return looksLikeCapture || isSharpScore;
        }
    }
}

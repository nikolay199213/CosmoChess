using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Engines;
using CosmoChess.Domain.Interface.Services;
using Microsoft.Extensions.Logging;

namespace CosmoChess.Infrastructure.Services
{
    public class BotService : IBotService
    {
        private readonly IEngineService _engineService;
        private readonly ILogger<BotService> _logger;
        private readonly Random _random = new();

        public BotService(IEngineService engineService, ILogger<BotService> logger)
        {
            _engineService = engineService;
            _logger = logger;
        }

        public async Task<string> GetBotMoveAsync(string fen, BotDifficulty difficulty, CancellationToken cancellationToken = default)
        {
            var depth = GetDepthForDifficulty(difficulty);
            var multiPv = GetMultiPvForDifficulty(difficulty);

            _logger.LogInformation("Bot calculating move for difficulty {Difficulty} at depth {Depth}, multiPv {MultiPv}",
                difficulty, depth, multiPv);

            try
            {
                // Use MultiPV to get multiple move options
                var analysis = await _engineService.AnalyzeMultiPvAsync(fen, depth, multiPv, cancellationToken);

                if (analysis.Lines == null || analysis.Lines.Count == 0)
                {
                    // Fallback to single best move
                    var move = await _engineService.AnalyzeAsync(fen, depth, cancellationToken);
                    return move;
                }

                // Choose move based on difficulty (add mistakes for weaker bots)
                var selectedMove = SelectMoveByDifficulty(analysis.Lines, difficulty);

                _logger.LogInformation("Bot calculated {LineCount} moves, selected: {Move}",
                    analysis.Lines.Count, selectedMove);

                return selectedMove;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating bot move");
                throw;
            }
        }

        private string SelectMoveByDifficulty(IReadOnlyList<Domain.ValueObject.AnalysisLine> lines, BotDifficulty difficulty)
        {
            // Beginner: 60% worst moves, 30% medium moves, 10% best move
            // Easy: 40% medium moves, 40% good moves, 20% best move
            // Medium: 20% medium moves, 50% good moves, 30% best move
            // Hard: 10% good moves, 90% best moves
            // Expert+: Always best move

            var rand = _random.Next(100);

            return difficulty switch
            {
                BotDifficulty.Beginner => rand switch
                {
                    < 60 => GetWorstMove(lines),      // 60% - Pick worst moves
                    < 90 => GetMediumMove(lines),     // 30% - Pick medium moves
                    _ => lines[0].Move                 // 10% - Best move
                },
                BotDifficulty.Easy => rand switch
                {
                    < 40 => GetMediumMove(lines),     // 40% - Pick medium moves
                    < 80 => GetGoodMove(lines),       // 40% - Pick good moves
                    _ => lines[0].Move                 // 20% - Best move
                },
                BotDifficulty.Medium => rand switch
                {
                    < 20 => GetMediumMove(lines),     // 20% - Pick medium moves
                    < 70 => GetGoodMove(lines),       // 50% - Pick good moves
                    _ => lines[0].Move                 // 30% - Best move
                },
                BotDifficulty.Hard => rand switch
                {
                    < 10 => GetGoodMove(lines),       // 10% - Pick good moves
                    _ => lines[0].Move                 // 90% - Best move
                },
                _ => lines[0].Move                     // Expert/Master - Always best
            };
        }

        private string GetWorstMove(IReadOnlyList<Domain.ValueObject.AnalysisLine> lines)
        {
            // Pick from worst 20% of moves
            var worstCount = Math.Max(1, lines.Count / 5);
            var startIndex = Math.Max(0, lines.Count - worstCount);
            var randomIndex = _random.Next(startIndex, lines.Count);
            return lines[randomIndex].Move;
        }

        private string GetMediumMove(IReadOnlyList<Domain.ValueObject.AnalysisLine> lines)
        {
            // Pick from middle 40% of moves
            var middleStart = lines.Count / 3;
            var middleEnd = (lines.Count * 2) / 3;
            if (middleStart >= middleEnd) return lines[Math.Min(1, lines.Count - 1)].Move;
            var randomIndex = _random.Next(middleStart, middleEnd);
            return lines[randomIndex].Move;
        }

        private string GetGoodMove(IReadOnlyList<Domain.ValueObject.AnalysisLine> lines)
        {
            // Pick from best 30% of moves (but not the absolute best)
            var goodCount = Math.Max(1, lines.Count / 3);
            var randomIndex = _random.Next(0, Math.Min(goodCount, lines.Count));
            return lines[randomIndex].Move;
        }

        private static int GetMultiPvForDifficulty(BotDifficulty difficulty)
        {
            // More options for weaker bots to create variety
            return difficulty switch
            {
                BotDifficulty.Beginner => 10,  // Analyze 10 moves
                BotDifficulty.Easy => 8,
                BotDifficulty.Medium => 5,
                BotDifficulty.Hard => 3,
                _ => 1                          // Expert/Master - only best move
            };
        }

        public int GetThinkingDelayMs(BotDifficulty difficulty)
        {
            // Add realistic thinking delay based on difficulty
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

        private static int GetDepthForDifficulty(BotDifficulty difficulty)
        {
            // Lower depth for beginner to avoid too strong play even with mistakes
            return difficulty switch
            {
                BotDifficulty.Beginner => 1,   // Very low depth + mistakes
                BotDifficulty.Easy => 3,
                BotDifficulty.Medium => 6,
                BotDifficulty.Hard => 10,
                BotDifficulty.Expert => 15,
                BotDifficulty.Master => 20,
                _ => 10
            };
        }
    }
}
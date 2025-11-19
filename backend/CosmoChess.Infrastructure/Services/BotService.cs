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

            _logger.LogInformation("Bot calculating move for difficulty {Difficulty} at depth {Depth}", difficulty, depth);

            try
            {
                var move = await _engineService.AnalyzeAsync(fen, depth, cancellationToken);

                _logger.LogInformation("Bot calculated move: {Move}", move);

                return move;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating bot move");
                throw;
            }
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
            return (int)difficulty;
        }
    }
}
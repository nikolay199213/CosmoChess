namespace CosmoChess.EngineService.Configuration
{
    public class EngineConfiguration
    {
        public string StockfishPath { get; set; } = string.Empty;
        public int StockfishHashSize { get; set; } = 1024;  // MB (default 1GB)
        public int StockfishThreads { get; set; } = 4;      // Number of threads
        public int StockfishDefaultDepth { get; set; } = 22; // Default analysis depth
        public int StockfishAnalysisTimeoutSeconds { get; set; } = 60; // Timeout for analysis
        public string RedisConnectionString { get; set; } = string.Empty;
        public int CacheExpirationMinutes { get; set; }

        public static EngineConfiguration FromConfiguration(IConfiguration configuration)
        {
            return new EngineConfiguration
            {
                StockfishPath = configuration["STOCKFISH_PATH"] ?? "stockfish",
                StockfishHashSize = GetConfigInt(configuration, "STOCKFISH_HASH_SIZE", 1024),
                StockfishThreads = GetConfigInt(configuration, "STOCKFISH_THREADS", 4),
                StockfishDefaultDepth = GetConfigInt(configuration, "STOCKFISH_DEFAULT_DEPTH", 22),
                StockfishAnalysisTimeoutSeconds = GetConfigInt(configuration, "STOCKFISH_TIMEOUT_SECONDS", 60),
                RedisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379",
                CacheExpirationMinutes = GetConfigInt(configuration, "CACHE_EXPIRATION_MINUTES", 1440)
            };
        }

        private static int GetConfigInt(IConfiguration configuration, string key, int defaultValue)
        {
            var value = configuration[key];
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}

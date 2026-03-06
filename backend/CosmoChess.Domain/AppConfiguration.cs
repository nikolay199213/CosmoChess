using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CosmoChess.Domain
{
    public class AppConfiguration
    {
        public string JwtKey { get; set; } = string.Empty;
        public string DatabaseConnectionString { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = "CosmoChess";
        public string JwtAudience { get; set; } = "CosmoChess";
        public string StockfishPath { get; set; } = string.Empty;

        // Stockfish engine configuration
        public int StockfishHashSize { get; set; } = 1024;  // MB (default 1GB)
        public int StockfishThreads { get; set; } = 4;      // Number of threads
        public int StockfishDefaultDepth { get; set; } = 22; // Default analysis depth
        public int StockfishAnalysisTimeoutSeconds { get; set; } = 60; // Timeout for analysis

        public bool IsDevelopment { get; set; }

        public static AppConfiguration FromConfiguration(IConfiguration configuration)
        {
            return new AppConfiguration
            {
                JwtKey = configuration["JWT_KEY"] ?? configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
                DatabaseConnectionString = configuration["DB_CONNECTION_STRING"]
                    ?? configuration.GetConnectionString("DefaultConnection")
                    ?? "Host=localhost;Database=cosmochess;Username=postgres;Password=password123",
                IsDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development",
                JwtIssuer = configuration["JWT_ISSUER"] ?? "CosmoChess",
                JwtAudience = configuration["JWT_AUDIENCE"] ?? "CosmoChess",
                StockfishPath = configuration["STOCKFISH_PATH"] ?? configuration["Stockfish:Path"] ?? "stockfish",

                // Stockfish engine parameters
                StockfishHashSize = GetConfigInt(configuration, "STOCKFISH_HASH_SIZE", 1024),
                StockfishThreads = GetConfigInt(configuration, "STOCKFISH_THREADS", 4),
                StockfishDefaultDepth = GetConfigInt(configuration, "STOCKFISH_DEFAULT_DEPTH", 22),
                StockfishAnalysisTimeoutSeconds = GetConfigInt(configuration, "STOCKFISH_TIMEOUT_SECONDS", 60)
            };
        }

        private static int GetConfigInt(IConfiguration configuration, string key, int defaultValue)
        {
            var value = configuration[key];
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }

    // Extension method для DI
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var config = AppConfiguration.FromConfiguration(configuration);
            services.AddSingleton(config);
            return services;
        }
    }
}

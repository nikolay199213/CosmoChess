﻿﻿using Microsoft.Extensions.DependencyInjection;

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

        public static AppConfiguration FromEnvironmentVariables()
        {
            var config = new AppConfiguration
            {
                JwtKey = GetEnvVar("JWT_KEY", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"),
                DatabaseConnectionString = GetEnvVar("DB_CONNECTION_STRING",
                    "Host=localhost;Database=cosmochess;Username=postgres;Password=password123"),
                IsDevelopment = GetEnvVar("ASPNETCORE_ENVIRONMENT") == "Development",
                JwtIssuer = GetEnvVar("JWT_ISSUER", "CosmoChess"),
                JwtAudience = GetEnvVar("JWT_AUDIENCE", "CosmoChess"),
                StockfishPath = GetEnvVar("STOCKFISH_PATH", "stockfish"),

                // Stockfish engine parameters with environment variable overrides
                StockfishHashSize = GetEnvVarInt("STOCKFISH_HASH_SIZE", 1024),
                StockfishThreads = GetEnvVarInt("STOCKFISH_THREADS", 4),
                StockfishDefaultDepth = GetEnvVarInt("STOCKFISH_DEFAULT_DEPTH", 22),
                StockfishAnalysisTimeoutSeconds = GetEnvVarInt("STOCKFISH_TIMEOUT_SECONDS", 60)
            };
            return config;
        }
        private static string GetRequiredEnvVar(string key)
        {
            return Environment.GetEnvironmentVariable(key)
                   ?? throw new InvalidOperationException($"Required environment variable {key} is not set");
        }

        private static string GetEnvVar(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }

        private static int GetEnvVarInt(string key, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
    // Extension method для DI
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppConfiguration(this IServiceCollection services)
        {
            var config = AppConfiguration.FromEnvironmentVariables();
            services.AddSingleton(config);
            return services;
        }
    }
}

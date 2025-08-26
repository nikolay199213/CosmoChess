﻿﻿using Microsoft.Extensions.DependencyInjection;

namespace CosmoChess.Domain
{
    public class AppConfiguration
    {
        public string JwtKey { get; set; } = string.Empty;
        public string DatabaseConnectionString { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = "CosmoChess";
        public string JwtAudience { get; set; } = "CosmoChess";

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
                JwtAudience = GetEnvVar("JWT_AUDIENCE", "CosmoChess")
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

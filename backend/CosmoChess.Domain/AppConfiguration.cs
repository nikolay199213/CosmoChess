using Microsoft.Extensions.DependencyInjection;

namespace CosmoChess.Domain
{
    public class AppConfiguration
    {
        public string JwtKey { get; set; } = string.Empty;
        public string DatabaseConnectionString { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = "MyAuthServer";
        public string JwtAudience { get; set; } = "MyAuthServer";

        public bool IsDevelopment { get; set; }

        public static AppConfiguration FromEnvironmentVariables()
        {
            var config = new AppConfiguration
            {
                JwtKey = GetRequiredEnvVar("JWT_KEY"),
                DatabaseConnectionString = GetRequiredEnvVar("DB_CONNECTION_STRING"),
                IsDevelopment = GetEnvVar("ASPNETCORE_ENVIRONMENT") == "Development"
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

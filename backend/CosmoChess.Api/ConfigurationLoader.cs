using CosmoChess.Domain;

namespace CosmoChess.Api
{
    public static class ConfigurationLoader
    {
        private static readonly Dictionary<string, string> SecretToEnvMapping = new()
        {
            // User Secret Key -> Environment Variable Name
            ["Jwt:Key"] = "JWT_KEY",
            ["ConnectionStrings:DefaultConnection"] = "DB_CONNECTION_STRING",
            ["Stockfish:Path"] = "STOCKFISH_PATH",
        };
        public static WebApplicationBuilder SetupConfiguration(this WebApplicationBuilder builder)
        {
            // Сначала загружаем все стандартные источники конфигурации
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
                TransferSecretsToEnvironmentVariables(builder.Configuration);
            }

            // Добавляем нашу конфигурацию
            builder.Services.AddAppConfiguration();

            return builder;
        }

        private static void TransferSecretsToEnvironmentVariables(IConfiguration configuration)
        {
            Console.WriteLine("Development mode: Loading secrets into environment variables...");

            foreach (var mapping in SecretToEnvMapping)
            {
                var secretKey = mapping.Key;
                var envVarName = mapping.Value;

                var value = configuration[secretKey];

                if (!string.IsNullOrEmpty(value))
                {
                    Environment.SetEnvironmentVariable(envVarName, value);
                    Console.WriteLine($"✅ {envVarName} loaded from user secrets");
                }
                else
                {
                    Console.WriteLine($"⚠️  {secretKey} not found in user secrets");
                }
            }
        }
    }
}

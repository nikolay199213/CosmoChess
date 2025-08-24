using CosmoChess.Domain;
using CosmoChess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CosmoChess.Api
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabase<TContext>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder>? additionalOptions = null) where TContext : DbContext
        {
            services.AddDbContext<TContext>((serviceProvider, options) =>
            {
                var config = serviceProvider.GetRequiredService<AppConfiguration>();

                // Основная настройка PostgreSQL
                options.UseNpgsql(config.DatabaseConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    // Можно добавить другие Npgsql-специфичные настройки
                    npgsqlOptions.CommandTimeout(30);
                });

                // Настройки для разработки
                if (config.IsDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }

                // Дополнительные настройки от вызывающего кода
                additionalOptions?.Invoke(options);
            });

            return services;
        }
        public static IServiceCollection AddCosmoChessDatabase(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder>? additionalOptions = null)
        {
            return services.AddDatabase<CosmoChessDbContext>(additionalOptions);
        }
    }
}

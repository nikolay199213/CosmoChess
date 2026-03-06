using CosmoChess.BotService.Configuration;
using CosmoChess.BotService.Kafka;
using CosmoChess.BotService.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
var botConfig = BotConfiguration.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(botConfig);

// HTTP Client for engine-service
builder.Services.AddHttpClient<IEngineClient, EngineHttpClient>(client =>
{
    client.BaseAddress = new Uri(botConfig.EngineServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Bot services
builder.Services.AddSingleton<BotMoveCalculator>();

// Kafka worker
builder.Services.AddHostedService<BotMoveWorker>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

await host.RunAsync();

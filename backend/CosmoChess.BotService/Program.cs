using CosmoChess.BotService.Configuration;
using CosmoChess.BotService.Kafka;
using CosmoChess.BotService.Services;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "bot-service"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSource("CosmoChess.Kafka")
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(builder.Configuration["OPEN_TELEMETRY_EXPORTER_OTLP_ENDPOINT"] ??
                                    "https://tempo-prod-10-prod-eu-west-2.grafana.net:443");
            opts.Headers =
                $"Authorization=Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                    $"{builder.Configuration["TEMPO_USER"]}:{builder.Configuration["TEMPO_API_KEY"]}"))}";
            opts.Protocol = OtlpExportProtocol.Grpc;
        }));


// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

await host.RunAsync();

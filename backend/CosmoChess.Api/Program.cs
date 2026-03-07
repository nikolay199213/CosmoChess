using CosmoChess.Api;
using CosmoChess.Api.Hubs;
using CosmoChess.Api.Services;
using CosmoChess.Application.Handlers;
using CosmoChess.Domain.Interface.Auth;
using CosmoChess.Domain.Interface.Engines;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Infrastructure.Auth;
using CosmoChess.Infrastructure.Engines;
using CosmoChess.Infrastructure.Kafka;
using CosmoChess.Infrastructure.Persistence;
using CosmoChess.Infrastructure.Repositories;
using CosmoChess.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.SetupConfiguration();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RegisterUserHandler>());

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebAndMobile", policy =>
    {
        policy.WithOrigins(
            "http://localhost:8080",           // Vite dev server
            "http://127.0.0.1:8080",
            "http://10.0.2.2:8080",            // Android emulator WebView
            "http://192.168.31.162:8080"       // Local network
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Для отладки
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});


builder.Services.AddCosmoChessDatabase();
builder.Services.AddJwtAuthentication();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Engine Service HTTP Client (replaces StockfishEngine)
var engineServiceUrl = builder.Configuration["ENGINE_SERVICE_URL"] ?? "http://engine-service:5001";
builder.Services.AddHttpClient<IEngineService, EngineHttpClient>(client =>
{
    client.BaseAddress = new Uri(engineServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Kafka Configuration
var kafkaBootstrapServers = builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:29092";
var kafkaGroupId = builder.Configuration["KAFKA_GROUP_ID"] ?? "game-service";
var kafkaRequestTopic = builder.Configuration["KAFKA_REQUEST_TOPIC"] ?? "bot-move-requests";
var kafkaResultTopic = builder.Configuration["KAFKA_RESULT_TOPIC"] ?? "bot-move-results";

// Kafka Producer for bot move requests
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<BotMoveProducer>>();
    return new BotMoveProducer(kafkaBootstrapServers, kafkaRequestTopic, logger);
});

// Kafka Consumer for bot move results
builder.Services.AddSingleton(sp =>
{
    var hubContext = sp.GetRequiredService<IHubContext<GameHub>>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    var logger = sp.GetRequiredService<ILogger<BotMoveResultConsumer>>();
    return new BotMoveResultConsumer(kafkaBootstrapServers, kafkaGroupId, kafkaResultTopic, hubContext, scopeFactory, logger);
});
builder.Services.AddHostedService(sp => sp.GetRequiredService<BotMoveResultConsumer>());

// Bot services (modified to use Kafka)
builder.Services.AddSingleton<BotMoveBackgroundService>();
builder.Services.AddSingleton<IBotMoveService>(sp => sp.GetRequiredService<BotMoveBackgroundService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BotMoveBackgroundService>());

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CosmoChess API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});


var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection only in production (handled by nginx/reverse proxy)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("AllowWebAndMobile");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/api/gamehub");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CosmoChessDbContext>();
    await context.Database.MigrateAsync();
}

app.Run();

using CosmoChess.EngineService.Configuration;
using CosmoChess.EngineService.Engines;
using CosmoChess.EngineService.Interfaces;
using CosmoChess.EngineService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CosmoChess Engine Service", Version = "v1" });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Load configuration from all sources (appsettings, env vars, user-secrets)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
var engineConfig = EngineConfiguration.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(engineConfig);

// Register Stockfish engine as singleton hosted service
builder.Services.AddSingleton<StockfishEngine>();
builder.Services.AddSingleton<IEngineService>(sp => sp.GetRequiredService<StockfishEngine>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<StockfishEngine>());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "engine-service" }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Analyze position (single best move)
app.MapPost("/analyze", async (AnalyzeRequest request, IEngineService engineService, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Fen))
            return Results.BadRequest(new { error = "FEN is required" });

        if (request.Depth <= 0 || request.Depth > 30)
            return Results.BadRequest(new { error = "Depth must be between 1 and 30" });

        var bestMove = await engineService.AnalyzeAsync(request.Fen, request.Depth, ct);
        return Results.Ok(new AnalyzeResponse(bestMove));
    }
    catch (TaskCanceledException)
    {
        return Results.StatusCode(408); // Request Timeout
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error analyzing position");
        return Results.StatusCode(500);
    }
})
.WithName("AnalyzePosition")
.WithTags("Analysis")
.Produces<AnalyzeResponse>(200)
.Produces(400)
.Produces(408)
.Produces(500);

// Analyze position with multiple lines (Multi-PV)
app.MapPost("/analyze-multipv", async (AnalyzeMultiPvRequest request, IEngineService engineService, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Fen))
            return Results.BadRequest(new { error = "FEN is required" });

        if (request.Depth <= 0 || request.Depth > 30)
            return Results.BadRequest(new { error = "Depth must be between 1 and 30" });

        if (request.MultiPv <= 0 || request.MultiPv > 10)
            return Results.BadRequest(new { error = "MultiPV must be between 1 and 10" });

        var result = await engineService.AnalyzeMultiPvAsync(request.Fen, request.Depth, request.MultiPv, ct);
        return Results.Ok(result);
    }
    catch (TaskCanceledException)
    {
        return Results.StatusCode(408); // Request Timeout
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error analyzing position with MultiPV");
        return Results.StatusCode(500);
    }
})
.WithName("AnalyzePositionMultiPv")
.WithTags("Analysis")
.Produces<AnalysisResult>(200)
.Produces(400)
.Produces(408)
.Produces(500);

app.Logger.LogInformation("Engine Service starting on port {Port}",
    builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5001");

app.Run();

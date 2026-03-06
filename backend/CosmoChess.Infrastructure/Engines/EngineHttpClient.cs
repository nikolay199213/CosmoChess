using System.Net.Http.Json;
using CosmoChess.Domain.Interface.Engines;
using CosmoChess.Domain.ValueObject;
using Microsoft.Extensions.Logging;

namespace CosmoChess.Infrastructure.Engines
{
    public class EngineHttpClient : IEngineService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EngineHttpClient> _logger;

        public EngineHttpClient(HttpClient httpClient, ILogger<EngineHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> AnalyzeAsync(string fen, int depth, CancellationToken cancellationToken)
        {
            try
            {
                var request = new { fen, depth };
                var response = await _httpClient.PostAsJsonAsync("/analyze", request, cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>(cancellationToken);

                if (result == null || string.IsNullOrEmpty(result.BestMove))
                    throw new InvalidOperationException("Engine service returned null or empty result");

                _logger.LogInformation("Engine analysis completed: best move {Move}", result.BestMove);

                return result.BestMove;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to communicate with engine service");
                throw;
            }
        }

        public async Task<AnalysisResult> AnalyzeMultiPvAsync(string fen, int depth, int multiPv, CancellationToken cancellationToken)
        {
            try
            {
                var request = new { fen, depth, multiPv };
                var response = await _httpClient.PostAsJsonAsync("/analyze-multipv", request, cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AnalysisResult>(cancellationToken);

                if (result == null)
                    throw new InvalidOperationException("Engine service returned null result");

                _logger.LogInformation("Engine MultiPV analysis completed: {LineCount} lines, depth {Depth}",
                    result.Lines.Count, result.Depth);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to communicate with engine service");
                throw;
            }
        }

        private class AnalyzeResponse
        {
            public string BestMove { get; set; } = string.Empty;
            public int Score { get; set; }
            public int Depth { get; set; }
        }
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using CosmoChess.BotService.Models;

namespace CosmoChess.BotService.Services
{
    public class EngineHttpClient : IEngineClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EngineHttpClient> _logger;

        public EngineHttpClient(HttpClient httpClient, ILogger<EngineHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AnalysisResult> AnalyzeMultiPvAsync(string fen, int depth, int multiPv, CancellationToken cancellationToken = default, Guid? gameId = null)
        {
            try
            {
                var requestBody = new { fen, depth, multiPv };
                var request = new HttpRequestMessage(HttpMethod.Post, "/analyze-multipv")
                {
                    Content = JsonContent.Create(requestBody)
                };

                if (gameId.HasValue)
                {
                    request.Headers.Add("X-Game-Id", gameId.Value.ToString());
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AnalysisResult>(cancellationToken);

                if (result == null)
                    throw new InvalidOperationException("Engine service returned null result");

                _logger.LogInformation("Engine analysis completed: {LineCount} lines, depth {Depth}",
                    result.Lines.Count, result.Depth);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to communicate with engine service");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize engine response");
                throw;
            }
        }
    }
}

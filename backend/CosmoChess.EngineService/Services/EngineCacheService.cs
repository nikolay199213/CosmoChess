using CosmoChess.EngineService.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CosmoChess.EngineService.Models;

namespace CosmoChess.EngineService.Services
{
    public class EngineCacheService(IDistributedCache cache, EngineConfiguration configuration, ILogger<EngineCacheService> logger)
    {

        public async Task<string?> GetAnalysis(string fen, int depth)
        {
            var bytes = await cache.GetAsync($"{fen}:{depth}");
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }
        public async Task SetAnalysis(string fen, int depth, string bestMove)
        {
            var bytes = Encoding.UTF8.GetBytes(bestMove);
            await cache.SetAsync($"{fen}:{depth}", bytes,
                new DistributedCacheEntryOptions
                    { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.CacheExpirationMinutes) });
        }
        public async Task<AnalysisResult?> GetMultiPv(string fen, int depth, int multiPv)
        {
            var bytes = await cache.GetAsync($"{fen}:{depth}:{multiPv}");
            if (bytes == null) return null;
            var stringResult = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<AnalysisResult>(stringResult);
        }
        public async Task SetMultiPv(string fen, int depth, int multiPv, AnalysisResult result)
        {
            var json = JsonSerializer.Serialize(result);
            var bytes = Encoding.UTF8.GetBytes(json);
            await cache.SetAsync($"{fen}:{depth}:{multiPv}", bytes,
                new DistributedCacheEntryOptions
                    { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.CacheExpirationMinutes) });
        }
    }
}

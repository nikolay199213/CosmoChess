using Microsoft.Extensions.Configuration;

namespace CosmoChess.BotService.Configuration
{
    public class BotConfiguration
    {
        public string KafkaBootstrapServers { get; set; } = "localhost:9092";
        public string KafkaGroupId { get; set; } = "bot-workers";
        public string RequestTopic { get; set; } = "bot-move-requests";
        public string ResultTopic { get; set; } = "bot-move-results";
        public string EngineServiceUrl { get; set; } = "http://engine-service:5001";

        public static BotConfiguration FromConfiguration(IConfiguration configuration)
        {
            return new BotConfiguration
            {
                KafkaBootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:29092",
                KafkaGroupId = configuration["KAFKA_GROUP_ID"] ?? "bot-workers",
                RequestTopic = configuration["KAFKA_REQUEST_TOPIC"] ?? "bot-move-requests",
                ResultTopic = configuration["KAFKA_RESULT_TOPIC"] ?? "bot-move-results",
                EngineServiceUrl = configuration["ENGINE_SERVICE_URL"] ?? "http://engine-service:5001"
            };
        }
    }
}

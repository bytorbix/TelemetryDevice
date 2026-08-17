using Confluent.Kafka;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;

namespace TelemetryDevice.KafkaBlock
{
    public class Kafka : IDisposable
    {
        private readonly ILogger<Kafka> _logger;
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;
        public ActionBlock<string> Block { get; }

        public Kafka(IConfiguration configuration, ILogger<Kafka> logger)
        {
            _logger = logger;
            _topic = configuration["Kafka:Topic"] ?? throw new InvalidOperationException("Kafka:Topic is not configured");

            ProducerConfig config = new()
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured")
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
            Block = new ActionBlock<string>(PublishAsync);
        }

        private async Task PublishAsync(string json)
        {
            JsonNode? root = JsonNode.Parse(json);
            if (root?["Tail number"] is not JsonNode tailNumberNode)
            {
                _logger.LogWarning("Dropping message: 'Tail number' field missing from parsed telemetry.");
                return;
            }
            string tailNumber = tailNumberNode.ToString();

            try
            {
                await _producer.ProduceAsync(_topic, new Message<string, string> { Key = tailNumber, Value = json });
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogWarning(ex, "Dropping message: Kafka produce failed for topic {Topic}.", _topic);
            }
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }
    }
}

using Confluent.Kafka;
using System.Threading.Tasks.Dataflow;

namespace TelemetryDevice.KafkaBlock
{
    public class Kafka : IDisposable
    {
        private readonly ILogger<Kafka> _logger;
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;
        public ActionBlock<string> Block { get; }

        public Kafka(IConfiguration configuration, ILogger<Kafka> logger)
        {
            _logger = logger;
            _topic = configuration["Kafka:Topic"] ?? throw new InvalidOperationException("Kafka:Topic is not configured");
            ProducerConfig config = new()
            {
                BootstrapServers = configuration["Kafka:BootStrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured")
            };
            _producer = new ProducerBuilder<Null, string>(config).Build();
            Block = new ActionBlock<string>(PublishAsync);
        }

        private async Task PublishAsync(string json)
        {
            try
            {
                await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
            }
            catch (ProduceException<Null, string> ex)
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

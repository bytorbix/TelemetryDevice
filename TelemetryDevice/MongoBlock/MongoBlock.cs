using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks.Dataflow;


namespace TelemetryDevice.MongoBlock
{
    public class Mongo
    {
        private readonly ILogger<Mongo> _logger;
        private readonly IMongoDatabase _database;
        public ActionBlock<string> Block { get; }

        public Mongo(IConfiguration configuration, ILogger<Mongo> logger)
        {
            _logger = logger;
            string connectionString = configuration["Mongo:ConnectionString"] ?? throw new InvalidOperationException("Mongo:ConnectionString is not configured");
            string databaseName = configuration["Mongo:Database"] ?? throw new InvalidOperationException("Mongo:Database is not configured");
            MongoClient client = new(connectionString);
            _database = client.GetDatabase(databaseName);
            Block = new ActionBlock<string>(InsertAsync); 
        }

        private async Task InsertAsync(string json)
        {
            try
            {
                BsonDocument document = BsonDocument.Parse(json);
                if (!document.TryGetValue("Tail number", out BsonValue tailNumberValue))
                {
                    _logger.LogWarning("Dropping message: 'Tail number' field missing from parsed telemetry.");
                    return;
                }
                string? tailNumber = tailNumberValue.ToString();
                if (string.IsNullOrEmpty(tailNumber))
                {
                    _logger.LogWarning("Dropping message: 'Tail number' value could not be converted to a collection name.");
                    return;
                }

                IMongoCollection<BsonDocument> collection = _database.GetCollection<BsonDocument>(tailNumber);
                await collection.InsertOneAsync(document);
            }
            catch (MongoException ex)
            {
                _logger.LogWarning(ex, "Dropping message: Mongo insert failed.");
            }
        }

        
        

    }
}

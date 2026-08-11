using System.Threading.Tasks.Dataflow;
using TelemetryDevice.Icd;
using TelemetryDevice.ParserBlock;

namespace TelemetryDevice.BuilderBlock
{
    public class Builder
    {
        private readonly ILogger<Builder> _logger;
        private readonly Parser _parser;
        public TransformManyBlock<byte[], byte[]> Block { get; }
        private readonly IcdParam _sync1; 
        private readonly IcdParam _sync2; 
        private readonly IcdParam _sync3; 
        private readonly IcdParam _tailNumber;

        public Builder(IcdDocument doc, ILogger<Builder> logger, Parser _parser)
        {
            _logger = logger;
            this._parser = _parser;
            _sync1 = doc.GetField("sync_1");
            _sync2 = doc.GetField("sync_2");
            _sync3 = doc.GetField("sync_3");
            _tailNumber = doc.GetField("Tail number");
            Block = new(payload => TryBuild(payload) ? new[] { payload } : Array.Empty<byte[]>());
            Block.LinkTo(_parser.Block);
        }

        private bool ValidateSyncByte(byte[] payload, IcdParam sync)
        {
            if (payload[sync.Location] == (byte)sync.Min) return true;

            _logger.LogWarning("Dropping payload: {Field} mismatch.", sync.Identifier);
            return false;
        }

        public bool TryBuild(byte[] payload)
        {
            if (payload == null) return false;
            if (!ValidateSyncByte(payload, _sync1)) return false;
            if (!ValidateSyncByte(payload, _sync2)) return false;
            if (!ValidateSyncByte(payload, _sync3)) return false;

            int paramValue = (payload[_tailNumber.Location + 1] << 8) | payload[_tailNumber.Location]; // 2 Byte value param
            if (!(paramValue >= _tailNumber.Min && paramValue <= _tailNumber.Max)) 
            {
                _logger.LogWarning("Dropping payload: {Field} mismatch.", _tailNumber.Identifier);
                return false;
            }

            return true;
        }


    }
}

using System.Threading.Tasks.Dataflow;
using TelemetryDevice.Icd;

namespace TelemetryDevice.BuilderBlock
{
    public class Builder
    {
        private readonly ILogger<Builder> _logger;
        public TransformManyBlock<byte[], byte[]> Block { get; }
        private readonly IcdParam _sync1;
        private readonly IcdParam _sync2;
        private readonly IcdParam _sync3;
        private readonly IcdParam _tailNumber;
        private int? _expectedTailNumber;

        public Builder(IcdDocument doc, ILogger<Builder> logger)
        {
            _logger = logger;
            _sync1 = doc.GetField("sync_1");
            _sync2 = doc.GetField("sync_2");
            _sync3 = doc.GetField("sync_3");
            _tailNumber = doc.GetField("Tail number");
            Block = new(payload => TryBuild(payload) ? new[] { payload } : Array.Empty<byte[]>());
        }

        public void SetExpectedTailNumber(int tailNumber)
        {
            _expectedTailNumber = tailNumber;
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
            try
            {
                if (!ValidateSyncByte(payload, _sync1) || !ValidateSyncByte(payload, _sync2) || !ValidateSyncByte(payload, _sync3))
                {
                    return false;
                }

                int paramValue = (payload[_tailNumber.Location + 1] << 8) | payload[_tailNumber.Location]; // 2 Byte value param
                if (!(paramValue >= _tailNumber.Min && paramValue <= _tailNumber.Max) || _expectedTailNumber != paramValue)
                {
                    _logger.LogWarning("Dropping payload: {Field} mismatch.", _tailNumber.Identifier);
                    return false;
                }

                return true;
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogWarning("Dropping payload: too short for expected ICD fields.");
                return false;
            }
        }


    }
}

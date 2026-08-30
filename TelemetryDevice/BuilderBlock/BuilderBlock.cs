using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using TelemetryDevice.Icd;
using TelemetryDevice.ListenerBlock;

namespace TelemetryDevice.BuilderBlock
{
    public class Builder
    {
        private readonly ILogger<Builder> _logger;
        public TransformManyBlock<CapturedPacket, CapturedPacket> Block { get; }
        private readonly IcdParam _sync1;
        private readonly IcdParam _sync2;
        private readonly IcdParam _sync3;
        private readonly IcdParam _tailNumber;
        private readonly ConcurrentDictionary<int, byte> _activeTailNumbers = new();

        public Builder(IcdDocument doc, ILogger<Builder> logger)
        {
            _logger = logger;
            _sync1 = doc.GetField("sync_1");
            _sync2 = doc.GetField("sync_2");
            _sync3 = doc.GetField("sync_3");
            _tailNumber = doc.GetField("Tail number");
            Block = new(captured => TryBuild(captured.payload) ? new[] { captured } : Array.Empty<CapturedPacket>());
        }

        public void AddTailNumber(int tailNumber)
        {
            _activeTailNumbers[tailNumber] = 0;
        }

        public void RemoveTailNumber(int tailNumber)
        {
            _activeTailNumbers.TryRemove(tailNumber, out _);
        }

        public IReadOnlyCollection<int> GetActiveTailNumbers()
        {
            return _activeTailNumbers.Keys.ToArray();
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
                if (!(paramValue >= _tailNumber.Min && paramValue <= _tailNumber.Max) || !_activeTailNumbers.ContainsKey(paramValue))
                {
                    _logger.LogWarning("Dropping payload: {Field} mismatch (received {Received}, expected {Expected}).", _tailNumber.Identifier, paramValue, _tailNumber);
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

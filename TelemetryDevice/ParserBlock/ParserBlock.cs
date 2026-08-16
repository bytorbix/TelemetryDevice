using System.Numerics;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;
using TelemetryDevice.Icd;
using TelemetryDevice.ListenerBlock;

namespace TelemetryDevice.ParserBlock
{
    public class Parser
    {
        private readonly IcdDocument _doc;
        private readonly IcdParam _correlator;
        private readonly ILogger<Parser> _logger;

        private const int BITS_PER_BYTE = 8;
        private const string CORRELATOR_PARAM_ID = "correlator";

        public TransformManyBlock<CapturedPacket, string> Block { get; }

        public Parser(IcdDocument doc, ILogger<Parser> logger)
        {
            _doc = doc;
            _logger = logger;
            _correlator = doc.GetField(CORRELATOR_PARAM_ID);
            Block = new TransformManyBlock<CapturedPacket, string>(captured => Parse(captured));
        }

        public IEnumerable<string> Parse(CapturedPacket captured)
        {
            try
            {
                int correlatorValue = ExtractBitField(captured.payload, _correlator);
                JsonObject result = new JsonObject();

                foreach (IcdParam param in _doc.Params)
                {
                    if (param.CorrValue != 0 && (param.CorrValue & correlatorValue) == 0)
                    {
                        continue;
                    }
                    result[param.Identifier] = DecodeField(captured.payload, param);
                }
                result["Timestamp"] = JsonValue.Create(captured.Timestamp);

                return new[] { result.ToJsonString() }; // parser output is json
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogWarning("Dropping payload: too short for expected ICD fields.");
                return Array.Empty<string>();
            }
        }

        private JsonValue DecodeField(byte[] payload, IcdParam param)
        {
            if (param.Size < BITS_PER_BYTE)
            {
                return JsonValue.Create(ExtractBitField(payload, param));
            }
            int bytes = param.Size / BITS_PER_BYTE;

            long value = 0;
            for (int i = bytes - 1; i >= 0; i--)
            {
                value = (value << BITS_PER_BYTE) | payload[param.Location + i];
            }

            return param.Type switch
            {
                IcdDataType.FLOAT => JsonValue.Create(BitConverter.Int32BitsToSingle((int)value)),
                IcdDataType.INTEGER => JsonValue.Create(value),
                _ => throw new NotSupportedException($"Unsupported ICD data type: {param.Type}")
            };
        }

        private int ExtractBitField(byte[] payload, IcdParam param)
        {
            byte maskByte = Convert.ToByte(param.Mask.Trim('\''), 2);
            int fieldByte = payload[param.Location] & maskByte;
            int shift = BitOperations.TrailingZeroCount(maskByte);
            return fieldByte >> shift;  
        }
    }
}

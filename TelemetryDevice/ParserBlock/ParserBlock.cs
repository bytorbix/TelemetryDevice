using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using TelemetryDevice.Icd;

namespace TelemetryDevice.ParserBlock
{
    public class Parser
    {
        private readonly IcdDocument _doc;
        private readonly IcdParam _correlator;

        private const int BITS_PER_BYTE = 8;
        private const string CORRELATOR_PARAM_ID = "correlator";

        public TransformBlock<byte[], string> Block { get; }

        public Parser(IcdDocument doc)
        {
            _doc = doc;
            _correlator = doc.GetField(CORRELATOR_PARAM_ID);
            Block = new TransformBlock<byte[], string>(payload => Parse(payload));
        }

        public string Parse(byte[] payload)
        {
            int correlatorValue = ExtractBitField(payload, _correlator);
            var result = new Dictionary<string, object>();

            foreach (IcdParam param in _doc.Params)
            {
                if (param.CorrValue != 0 && (param.CorrValue & correlatorValue) == 0)
                {
                    continue;
                }
                result[param.Identifier] = DecodeField(payload, param);
            }

            return JsonSerializer.Serialize(result); // parser output is json
        }

        private object DecodeField(byte[] payload, IcdParam param)
        {
            if (param.Size < BITS_PER_BYTE)
            {
                return ExtractBitField(payload, param);
            }
            int bytes = param.Size / BITS_PER_BYTE;

            long value = 0;
            for (int i = bytes - 1; i >= 0; i--)
            {
                value = (value << BITS_PER_BYTE) | payload[param.Location + i];
            }

            if (param.Type == IcdDataType.FLOAT)
            {
                return BitConverter.Int32BitsToSingle((int)value);
            }

            return value;
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

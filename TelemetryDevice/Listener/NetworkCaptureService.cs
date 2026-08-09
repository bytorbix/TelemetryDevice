using SharpPcap;

namespace TelemetryDevice.Listener
{
    // Owns picking + opening the capture device. Precedence: explicit config first,
    // then loopback (\Device\NPF_Loopback) as a dev-convenience fallback, else throw.
    public class NetworkCaptureService
    {
        private readonly IConfiguration _configuration;
        private readonly string DEVICE_NAME_FIELD="DeviceName";
        private readonly string FALLBACK_DEVICE_NAME="\\Device\\NPF_Loopback";

        public NetworkCaptureService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ICaptureDevice SelectDevice()
        {
            var configuredName = _configuration[DEVICE_NAME_FIELD];

            if (configuredName != null)
            {
                ICaptureDevice? device = CaptureDeviceList.Instance.FirstOrDefault(d => d.Name == configuredName);
                if (device != null)
                {
                    return device; 
                } else
                {
                    throw new InvalidOperationException($"No capture device found with name '{configuredName}'");
                }
            }

            ICaptureDevice? fallback = CaptureDeviceList.Instance.FirstOrDefault(d => d.Name == FALLBACK_DEVICE_NAME);
            if (fallback == null)
            {
                throw new InvalidOperationException("No capture device available: no config, no loopback adapter found.");
            }
            else
            {
                return fallback;
            }
        }

        public void Open(ICaptureDevice device, PacketArrivalEventHandler handler, string filter="udp and port 5000")
        {

            device.Open(DeviceModes.Promiscuous, 1000);
            device.Filter = filter;
            device.OnPacketArrival += handler;
        }

        public void Start(ICaptureDevice device)
        {
            device.StartCapture();
        }

        public void Close(ICaptureDevice device)
        {
            device.StopCapture();
            device.Close();
        }
    }
}

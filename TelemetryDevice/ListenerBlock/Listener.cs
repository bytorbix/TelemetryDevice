using PacketDotNet;
using SharpPcap;
using System.Threading.Tasks.Dataflow;

namespace TelemetryDevice.ListenerBlock
{
    public class Listener
    {
        private readonly NetworkCaptureService _captureService;
        private readonly ILogger<Listener> _logger;
        private ICaptureDevice? _activeDevice;

        public BufferBlock<byte[]> Block { get; } = new();

        public Listener(NetworkCaptureService captureService, ILogger<Listener> logger)
        {
            _captureService = captureService;
            _logger = logger;
        }

        public void StartListening(string ip, int port)
        {
            if (_activeDevice != null)
            {
                throw new InvalidOperationException("Already listening on a device.");
            }

            if (ip == null || (port < 0 || port > 65535))
            {
                throw new ArgumentException("Bad Input");
            }

            string filter = $"udp and host {ip} and port {port}";
            _activeDevice = _captureService.SelectDevice();

            try
            {
                _captureService.Open(_activeDevice, OnPacketArrivalHandler, filter);
                _captureService.Start(_activeDevice);
            }
            catch (SharpPcap.PcapException)
            {
                _activeDevice = null;
                throw;
            }
        }

        public void OnPacketArrivalHandler(object sender, PacketCapture e)
        {
            try
            {
                RawCapture rawPacket = e.GetPacket();
                Packet packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                UdpPacket? udpPacket = packet.Extract<UdpPacket>();
                if (udpPacket == null) { return; }
                byte[] payload = udpPacket.PayloadData;
                Block.Post(payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dropping capture: failed to parse raw packet.");
            }
        }

        public void StopListening()
        {
            if (_activeDevice == null)
            {
                throw new InvalidOperationException("Not currently listening on any device.");
            }
            _captureService.Close(_activeDevice);
            _activeDevice = null;
        }
    }
}

using PacketDotNet;
using SharpPcap;

namespace TelemetryDevice.Listener
{
    public class Listener
    {
        private readonly NetworkCaptureService _captureService;
        private ICaptureDevice? _activeDevice;

        public Listener(NetworkCaptureService captureService)
        {
            _captureService = captureService;
        }

        public void StartListening(string ip, int port)
        {
            if (_activeDevice != null)
            {
                throw new InvalidOperationException("Already listening on a device.");
            }

            string filter = $"udp and host {ip} and port {port}";

            _activeDevice = _captureService.SelectDevice();
            _captureService.Open(_activeDevice, OnPacketArrivalHandler, filter);
            _captureService.Start(_activeDevice);
        }

        public void OnPacketArrivalHandler(object sender, PacketCapture e)
        {
            RawCapture rawPacket = e.GetPacket();
            Packet packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            UdpPacket? udpPacket = packet.Extract<UdpPacket>();
            if (udpPacket == null) { return; }
            byte[] payload = udpPacket.PayloadData;


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

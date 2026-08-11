using PacketDotNet;
using SharpPcap;
using System.Threading.Tasks.Dataflow;
using TelemetryDevice.BuilderBlock;

namespace TelemetryDevice.ListenerBlock
{
    public class Listener
    {
        private readonly NetworkCaptureService _captureService;
        private ICaptureDevice? _activeDevice;

        private readonly Builder _builder;

        public Listener(NetworkCaptureService captureService, Builder _builder)
        {
            _captureService = captureService;
            this._builder = _builder;
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
            _builder.Block.Post(payload); // pipeline the payload into the Builder

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

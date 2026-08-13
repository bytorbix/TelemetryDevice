namespace TelemetryDevice.ListenerBlock
{
    public record CapturedPacket(byte[] payload, DateTime Timestamp);
}

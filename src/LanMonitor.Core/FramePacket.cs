namespace LanMonitor.Core;

/// <summary>
/// 阶段二主协议：4 字节小端长度 + JPEG 整帧。
/// </summary>
public static class FramePacket
{
    public static byte[] Wrap(ReadOnlySpan<byte> jpegPayload)
    {
        if (jpegPayload.Length < 2)
        {
            throw new ArgumentException("JPEG payload too short.", nameof(jpegPayload));
        }

        var packet = new byte[4 + jpegPayload.Length];
        BitConverter.TryWriteBytes(packet.AsSpan(0, 4), jpegPayload.Length);
        jpegPayload.CopyTo(packet.AsSpan(4));
        return packet;
    }

    public static async Task WriteAsync(Stream stream, ReadOnlyMemory<byte> jpegPayload, CancellationToken cancellationToken = default)
    {
        if (jpegPayload.Length < 2)
        {
            throw new ArgumentException("JPEG payload too short.", nameof(jpegPayload));
        }

        var header = BitConverter.GetBytes(jpegPayload.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(jpegPayload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}

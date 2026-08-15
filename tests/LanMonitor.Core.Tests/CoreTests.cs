using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using LanMonitor.Core;
using System.Net.Sockets;

namespace LanMonitor.Core.Tests;

public class FrameAssemblerTests
{
    [Fact]
    public void Assembles_jpeg_split_across_packets()
    {
        var jpeg = MakeJpeg(32, 24, 80);
        var assembler = new FrameAssembler();
        var mid = jpeg.Length / 2;
        Assert.Empty(assembler.Push(jpeg.AsSpan(0, mid)));
        var frames = assembler.Push(jpeg.AsSpan(mid));
        Assert.Single(frames);
        Assert.Equal(jpeg, frames[0]);
    }

    [Fact]
    public void Assembles_bmp_using_header_length()
    {
        var bmp = MakeBmp(40, 30);
        Assert.True(bmp.Length > 1000);
        var assembler = new FrameAssembler();
        var chunk = bmp.Length / 3;
        Assert.Empty(assembler.Push(bmp.AsSpan(0, chunk)));
        Assert.Empty(assembler.Push(bmp.AsSpan(chunk, chunk)));
        var frames = assembler.Push(bmp.AsSpan(chunk * 2));
        Assert.Single(frames);
        Assert.Equal(bmp.Length, frames[0].Length);
        Assert.Equal((byte)'B', frames[0][0]);
        Assert.Equal((byte)'M', frames[0][1]);
    }

    [Fact]
    public void Assembles_two_concatenated_jpegs_separately()
    {
        var a = MakeJpeg(20, 20, 75);
        var b = MakeJpeg(22, 18, 65);
        var all = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, all, 0, a.Length);
        Buffer.BlockCopy(b, 0, all, a.Length, b.Length);
        var assembler = new FrameAssembler();
        var frames = assembler.Push(all);
        Assert.Equal(2, frames.Count);
        Assert.Equal(a, frames[0]);
        Assert.Equal(b, frames[1]);
    }

    [Fact]
    public void Assembles_length_prefixed_bmp()
    {
        var bmp = MakeBmp(48, 36);
        var packet = new byte[4 + bmp.Length];
        BitConverter.GetBytes(bmp.Length).CopyTo(packet, 0);
        Buffer.BlockCopy(bmp, 0, packet, 4, bmp.Length);
        var assembler = new FrameAssembler();
        var mid = packet.Length / 2;
        Assert.Empty(assembler.Push(packet.AsSpan(0, mid)));
        var frames = assembler.Push(packet.AsSpan(mid));
        Assert.Single(frames);
        Assert.Equal(bmp, frames[0]);
    }

    [Fact]
    public void Does_not_treat_ffd8_inside_bmp_pixels_as_jpeg()
    {
        using var image = new Image<Rgb24>(64, 48);
        image[3, 3] = new Rgb24(255, 216, 0);
        image[4, 3] = new Rgb24(255, 217, 0);
        using var ms = new MemoryStream();
        image.Save(ms, new BmpEncoder());
        var bmp = ms.ToArray();
        var assembler = new FrameAssembler();
        var frames = assembler.Push(bmp);
        Assert.Single(frames);
        Assert.Equal((byte)'B', frames[0][0]);
        Assert.Equal((byte)'M', frames[0][1]);
        Assert.Equal(bmp.Length, frames[0].Length);
    }

    [Fact]
    public void Ignores_incomplete_jpeg_without_eoi()
    {
        var jpeg = MakeJpeg(16, 16, 70);
        var cut = jpeg.Length - 2;
        var assembler = new FrameAssembler();
        Assert.Empty(assembler.Push(jpeg.AsSpan(0, cut)));
    }

    private static byte[] MakeJpeg(int w, int h, int quality)
    {
        using var image = new Image<Rgb24>(w, h);
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = quality });
        return ms.ToArray();
    }

    private static byte[] MakeBmp(int w, int h)
    {
        using var image = new Image<Rgb24>(w, h);
        using var ms = new MemoryStream();
        image.Save(ms, new BmpEncoder());
        return ms.ToArray();
    }
}

public class SaveSchedulerTests
{
    [Fact]
    public void First_call_saves_then_respects_interval()
    {
        var scheduler = new SaveScheduler { IntervalSeconds = 1.0 };
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(scheduler.ShouldSave(t0));
        Assert.False(scheduler.ShouldSave(t0.AddMilliseconds(999)));
        Assert.True(scheduler.ShouldSave(t0.AddSeconds(1)));
    }

    [Fact]
    public void Interval_change_on_ui_takes_effect_immediately()
    {
        var scheduler = new SaveScheduler { IntervalSeconds = 5 };
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(scheduler.ShouldSave(t0));
        scheduler.IntervalSeconds = 0.2;
        Assert.False(scheduler.ShouldSave(t0.AddMilliseconds(100)));
        Assert.True(scheduler.ShouldSave(t0.AddMilliseconds(200)));
    }
}

public class IpFilterTests
{
    [Fact]
    public void Empty_allow_list_permits_all()
    {
        Assert.True(IpFilter.IsAllowed("192.168.111.59:31112", ""));
    }

    [Fact]
    public void Extracts_ipv4_and_matches_list()
    {
        Assert.Equal("192.168.111.59", IpFilter.ExtractIp("192.168.111.59:31112"));
        Assert.True(IpFilter.IsAllowed("192.168.111.59:1", "192.168.111.59"));
        Assert.False(IpFilter.IsAllowed("192.168.111.152:1", "192.168.111.59"));
    }
}

public class JpegFileSaverTests
{
    [Fact]
    public void Compresses_bmp_to_much_smaller_jpeg()
    {
        using var image = new Image<Rgb24>(320, 240);
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                image[x, y] = new Rgb24((byte)x, (byte)y, 80);
            }
        }

        using var bmpStream = new MemoryStream();
        image.Save(bmpStream, new BmpEncoder());
        var bmp = bmpStream.ToArray();
        Assert.True(bmp.Length > 200_000, $"bmp={bmp.Length}");

        var dir = Path.Combine(Path.GetTempPath(), "lan-monitor-tests");
        var path = JpegFileSaver.BuildPath(dir, 1);
        var result = JpegFileSaver.Save(bmp, path, 60);
        Assert.True(result.Success, result.Error);
        Assert.True(File.Exists(path));
        Assert.True(result.Bytes > 1000);
        Assert.True(result.Bytes < bmp.Length / 2, $"jpeg={result.Bytes} bmp={bmp.Length}");
    }
}

public class ImagePayloadTests
{
    [Fact]
    public void Unwraps_four_byte_length_prefix()
    {
        var bmp = MakeSmallBmp();
        var packet = new byte[4 + bmp.Length];
        BitConverter.GetBytes(bmp.Length).CopyTo(packet, 0);
        Buffer.BlockCopy(bmp, 0, packet, 4, bmp.Length);
        Assert.True(ImagePayload.TryUnwrap(packet, out var image));
        Assert.Equal((byte)'B', image[0]);
        Assert.True(ImagePayload.TryEncodeJpeg(packet, 60, out var jpeg, out var error), error);
        Assert.True(jpeg.Length > 100);
        Assert.True(jpeg.Length < bmp.Length);
    }

    [Fact]
    public void Wraps_raw_dib_header()
    {
        var bmp = MakeSmallBmp();
        var dib = bmp.AsSpan(14).ToArray();
        Assert.Equal(40, BitConverter.ToInt32(dib, 0));
        Assert.True(ImagePayload.TryUnwrap(dib, out var wrapped));
        Assert.Equal((byte)'B', wrapped[0]);
        Assert.Equal((byte)'M', wrapped[1]);
    }

    private static byte[] MakeSmallBmp()
    {
        using var image = new Image<Rgb24>(32, 24);
        using var ms = new MemoryStream();
        image.Save(ms, new BmpEncoder());
        return ms.ToArray();
    }
}

public class TcpReceiveServerTests
{
    [Fact]
    public async Task Receives_chunked_jpeg_over_tcp()
    {
        var jpeg = MakeJpeg();
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new TcpReceiveServer { Port = 0, ReceiveEnabled = true };
        server.FrameReceived += frame => received.TrySetResult(frame);
        server.Start();
        Assert.True(server.BoundPort > 0);

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(System.Net.IPAddress.Loopback, server.BoundPort);
            var stream = client.GetStream();
            var mid = jpeg.Length / 2;
            await stream.WriteAsync(jpeg.AsMemory(0, mid));
            await stream.FlushAsync();
            await Task.Delay(50);
            await stream.WriteAsync(jpeg.AsMemory(mid));
            await stream.FlushAsync();

            var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
            Assert.True(completed == received.Task, "timed out waiting for frame");
            var frame = await received.Task;
            Assert.Equal(jpeg.Length, frame.Length);
            Assert.Equal(0xFF, frame[0]);
            Assert.Equal(0xD8, frame[1]);
        }

        server.Stop();
    }

    [Fact]
    public async Task Receives_length_prefixed_jpeg_over_tcp()
    {
        var jpeg = MakeJpeg();
        Assert.True(jpeg.Length >= 54);
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new TcpReceiveServer { Port = 0, ReceiveEnabled = true };
        server.FrameReceived += frame => received.TrySetResult(frame);
        server.Start();

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(System.Net.IPAddress.Loopback, server.BoundPort);
            await FramePacket.WriteAsync(client.GetStream(), jpeg);
            var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
            Assert.True(completed == received.Task, "timed out waiting for prefixed frame");
            var frame = await received.Task;
            Assert.Equal(jpeg, frame);
        }

        server.Stop();
    }

    private static byte[] MakeJpeg()
    {
        using var image = new Image<Rgb24>(64, 48);
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 70 });
        return ms.ToArray();
    }
}

public class FramePacketTests
{
    [Fact]
    public void Wrap_prefixes_little_endian_length()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0x00, 0x01, 0xFF, 0xD9 };
        var packet = FramePacket.Wrap(jpeg);
        Assert.Equal(4 + jpeg.Length, packet.Length);
        Assert.Equal(jpeg.Length, BitConverter.ToInt32(packet, 0));
        Assert.Equal(jpeg, packet.AsSpan(4).ToArray());
    }

    [Fact]
    public void Assembler_accepts_wrapped_jpeg_split_across_writes()
    {
        using var image = new Image<Rgb24>(80, 60);
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 65 });
        var jpeg = ms.ToArray();
        Assert.True(jpeg.Length >= 54);

        var packet = FramePacket.Wrap(jpeg);
        var assembler = new FrameAssembler();
        var mid = packet.Length / 3;
        Assert.Empty(assembler.Push(packet.AsSpan(0, mid)));
        Assert.Empty(assembler.Push(packet.AsSpan(mid, mid)));
        var frames = assembler.Push(packet.AsSpan(mid * 2));
        Assert.Single(frames);
        Assert.Equal(jpeg, frames[0]);
    }
}

public class ReconnectGateTests
{
    [Fact]
    public void Unlimited_always_allows_and_counts()
    {
        var gate = new ReconnectGate { Unlimited = true, MaxAttempts = 2 };
        for (var i = 1; i <= 20; i++)
        {
            Assert.True(gate.TryBeginAttempt());
            Assert.Equal(i, gate.AttemptsUsed);
        }
    }

    [Fact]
    public void Limited_allows_five_attempts_then_blocks()
    {
        var gate = new ReconnectGate { Unlimited = false, MaxAttempts = 5 };
        for (var i = 1; i <= 5; i++)
        {
            Assert.True(gate.TryBeginAttempt());
            Assert.Equal(i, gate.AttemptsUsed);
        }

        Assert.False(gate.TryBeginAttempt());
        Assert.Equal(5, gate.AttemptsUsed);
        Assert.Equal(0, gate.AttemptsRemaining);
    }

    [Fact]
    public void Zero_max_disables_reconnect_when_limited()
    {
        var gate = new ReconnectGate { Unlimited = false, MaxAttempts = 0 };
        Assert.False(gate.CanAttempt);
        Assert.False(gate.TryBeginAttempt());
    }

    [Fact]
    public void Reset_clears_used_count()
    {
        var gate = new ReconnectGate { Unlimited = false, MaxAttempts = 2 };
        Assert.True(gate.TryBeginAttempt());
        Assert.True(gate.TryBeginAttempt());
        Assert.False(gate.TryBeginAttempt());
        gate.Reset();
        Assert.Equal(0, gate.AttemptsUsed);
        Assert.True(gate.TryBeginAttempt());
    }
}

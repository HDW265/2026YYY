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

    private static byte[] MakeJpeg()
    {
        using var image = new Image<Rgb24>(24, 24);
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 70 });
        return ms.ToArray();
    }
}

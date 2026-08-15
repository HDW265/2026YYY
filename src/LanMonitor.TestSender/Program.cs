using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net.Sockets;
using LanMonitor.Core;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 19730;
var fps = args.Length > 2 ? int.Parse(args[2]) : 5;

using var image = new Image<Rgb24>(640, 360);
for (var y = 0; y < image.Height; y++)
{
    for (var x = 0; x < image.Width; x++)
    {
        image[x, y] = new Rgb24((byte)(x + Environment.TickCount), (byte)y, 90);
    }
}

using var ms = new MemoryStream();
image.Save(ms, new JpegEncoder { Quality = 60 });
var jpeg = ms.ToArray();

using var client = new TcpClient();
await client.ConnectAsync(host, port);
var stream = client.GetStream();
Console.WriteLine($"connected {host}:{port} jpeg={jpeg.Length} (length-prefixed)");

for (var i = 0; i < 8; i++)
{
    await FramePacket.WriteAsync(stream, jpeg);
    Console.WriteLine("sent frame " + (i + 1));
    await Task.Delay(1000 / Math.Max(1, fps));
}

Console.WriteLine("done");

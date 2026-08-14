using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net.Sockets;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 13689;
var format = args.Length > 2 ? args[2] : "jpeg";
var fps = 5;

using var image = new Image<Rgb24>(320, 200);
for (var y = 0; y < image.Height; y++)
{
    for (var x = 0; x < image.Width; x++)
    {
        image[x, y] = new Rgb24((byte)(x + Environment.TickCount), (byte)y, 90);
    }
}

using var ms = new MemoryStream();
if (string.Equals(format, "bmp", StringComparison.OrdinalIgnoreCase))
{
    image.Save(ms, new BmpEncoder());
}
else
{
    image.Save(ms, new JpegEncoder { Quality = 60 });
}

var payload = ms.ToArray();
using var client = new TcpClient();
await client.ConnectAsync(host, port);
var stream = client.GetStream();
Console.WriteLine($"connected {host}:{port} frame={payload.Length} {format}");

for (var i = 0; i < 8; i++)
{
    var offset = 0;
    while (offset < payload.Length)
    {
        var n = Math.Min(4096, payload.Length - offset);
        await stream.WriteAsync(payload.AsMemory(offset, n));
        offset += n;
    }

    await stream.FlushAsync();
    Console.WriteLine("sent frame " + (i + 1));
    await Task.Delay(1000 / fps);
}

Console.WriteLine("done");

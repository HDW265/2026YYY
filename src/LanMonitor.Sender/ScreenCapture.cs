using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LanMonitor.Sender;

internal static class ScreenCapture
{
    public static byte[] CapturePrimaryJpeg(int quality, int maxEdge)
    {
        var screen = Screen.PrimaryScreen ?? throw new InvalidOperationException("无可用主显示器。");
        var bounds = screen.Bounds;
        using var source = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(source))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }

        using var framed = ScaleIfNeeded(source, maxEdge);
        return EncodeJpeg(framed, quality);
    }

    private static Bitmap ScaleIfNeeded(Bitmap source, int maxEdge)
    {
        if (maxEdge <= 0)
        {
            return (Bitmap)source.Clone();
        }

        var longest = Math.Max(source.Width, source.Height);
        if (longest <= maxEdge)
        {
            return (Bitmap)source.Clone();
        }

        var scale = maxEdge / (double)longest;
        var w = Math.Max(1, (int)Math.Round(source.Width * scale));
        var h = Math.Max(1, (int)Math.Round(source.Height * scale));
        var scaled = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(scaled);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, 0, 0, w, h);
        return scaled;
    }

    private static byte[] EncodeJpeg(Bitmap bitmap, int quality)
    {
        quality = Math.Clamp(quality, 1, 100);
        var codec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid)
            ?? throw new InvalidOperationException("系统缺少 JPEG 编码器。");

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        using var ms = new MemoryStream();
        bitmap.Save(ms, codec, parameters);
        return ms.ToArray();
    }
}

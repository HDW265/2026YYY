using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LanMonitor.Sender;

internal static class ScreenCapture
{
    /// <summary>用于把截屏切回 UI/STA 线程（如热键宿主窗）。</summary>
    public static Control? UiMarshal { get; set; }

    public static byte[] CapturePrimaryJpeg(int quality, int maxEdge)
    {
        var marshal = UiMarshal;
        if (marshal is { IsHandleCreated: true } && marshal.InvokeRequired)
        {
            return (byte[])marshal.Invoke(new Func<byte[]>(() => CaptureWithRetry(quality, maxEdge)))!;
        }

        return CaptureWithRetry(quality, maxEdge);
    }

    private static byte[] CaptureWithRetry(int quality, int maxEdge)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return CapturePrimaryJpegCore(quality, maxEdge);
            }
            catch (Exception ex)
            {
                last = ex;
                Thread.Sleep(40 + attempt * 40);
            }
        }

        throw last ?? new InvalidOperationException("截屏失败。");
    }

    private static byte[] CapturePrimaryJpegCore(int quality, int maxEdge)
    {
        var screen = Screen.PrimaryScreen ?? throw new InvalidOperationException("无可用主显示器。");
        var bounds = screen.Bounds;
        using var source = CaptureDesktopBitmap(bounds);
        using var framed = ScaleIfNeeded(source, maxEdge);
        return EncodeJpeg(framed, quality);
    }

    private static Bitmap CaptureDesktopBitmap(Rectangle bounds)
    {
        var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        var desktop = GetDC(IntPtr.Zero);
        if (desktop == IntPtr.Zero)
        {
            bmp.Dispose();
            throw new InvalidOperationException("无法获取桌面 DC。");
        }

        try
        {
            using var g = Graphics.FromImage(bmp);
            var hdcDest = g.GetHdc();
            try
            {
                if (!BitBlt(hdcDest, 0, 0, bounds.Width, bounds.Height,
                        desktop, bounds.X, bounds.Y, 0x00CC0020 /* SRCCOPY */))
                {
                    throw new InvalidOperationException("BitBlt 失败（句柄无效或桌面不可用）。");
                }
            }
            finally
            {
                g.ReleaseHdc(hdcDest);
            }
        }
        catch
        {
            bmp.Dispose();
            throw;
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, desktop);
        }

        return bmp;
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(
        IntPtr hdcDest, int xDest, int yDest, int width, int height,
        IntPtr hdcSrc, int xSrc, int ySrc, int rop);
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace LanMonitor.Core;

/// <summary>
/// 易语言「发送数据」常带 4 字节长度或其它短包头，后面才是 BMP/JPEG/DIB。
/// </summary>
public static class ImagePayload
{
    public static bool TryUnwrap(byte[] raw, out byte[] imageBytes)
    {
        imageBytes = Array.Empty<byte>();
        if (raw.Length < 54)
        {
            return false;
        }

        if (LooksLikeImage(raw, 0))
        {
            imageBytes = MaybeWrapDib(raw);
            return true;
        }

        if (raw.Length >= 8)
        {
            var prefixed = BitConverter.ToInt32(raw, 0);
            if (prefixed >= 54 && prefixed <= raw.Length - 4 && LooksLikeImage(raw, 4))
            {
                imageBytes = MaybeWrapDib(raw.AsSpan(4, prefixed).ToArray());
                return true;
            }
        }

        var marker = FindImageStart(raw, 64);
        if (marker >= 0)
        {
            imageBytes = MaybeWrapDib(raw.AsSpan(marker).ToArray());
            return true;
        }

        return false;
    }

    public static bool TryEncodeJpeg(byte[] raw, int quality, out byte[] jpeg, out string error)
    {
        jpeg = Array.Empty<byte>();
        error = string.Empty;
        if (!TryUnwrap(raw, out var imageBytes))
        {
            error = "不是可识别的图片数据（可能仍带组件包头）";
            return false;
        }

        try
        {
            if (imageBytes.Length >= 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes.Length < 1_500_000)
            {
                jpeg = imageBytes;
                return true;
            }

            using var image = Image.Load(imageBytes);
            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder { Quality = Math.Clamp(quality, 1, 100) });
            jpeg = ms.ToArray();
            return jpeg.Length > 100;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool LooksLikeImage(byte[] data, int offset)
    {
        if (offset + 2 > data.Length)
        {
            return false;
        }

        if (data[offset] == 0xFF && data[offset + 1] == 0xD8)
        {
            return true;
        }

        if (data[offset] == (byte)'B' && data[offset + 1] == (byte)'M')
        {
            return true;
        }

        return offset + 4 <= data.Length && BitConverter.ToInt32(data, offset) == 40;
    }

    public static int FindImageStart(byte[] data, int maxScan)
    {
        var limit = Math.Min(data.Length - 2, maxScan);
        for (var i = 0; i <= limit; i++)
        {
            if (LooksLikeImage(data, i))
            {
                return i;
            }
        }

        return -1;
    }

    private static byte[] MaybeWrapDib(byte[] data)
    {
        if (data.Length >= 4 && data[0] == (byte)'B' && data[1] == (byte)'M')
        {
            return data;
        }

        if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return data;
        }

        if (data.Length >= 40 && BitConverter.ToInt32(data, 0) == 40)
        {
            var fileSize = 14 + data.Length;
            var bmp = new byte[fileSize];
            bmp[0] = (byte)'B';
            bmp[1] = (byte)'M';
            BitConverter.GetBytes(fileSize).CopyTo(bmp, 2);
            BitConverter.GetBytes(14).CopyTo(bmp, 10);
            Buffer.BlockCopy(data, 0, bmp, 14, data.Length);
            return bmp;
        }

        return data;
    }
}

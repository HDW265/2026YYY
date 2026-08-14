using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace LanMonitor.Core;

public static class JpegFileSaver
{
    public static int ClampQuality(int quality) => Math.Clamp(quality, 1, 100);

    public static string BuildPath(string directory, int sequence)
    {
        directory = NormalizeDirectory(directory);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{sequence}.jpg");
    }

    public static string NormalizeDirectory(string directory)
    {
        directory = directory.Trim();
        if (!directory.EndsWith(Path.DirectorySeparatorChar) &&
            !directory.EndsWith(Path.AltDirectorySeparatorChar))
        {
            directory += Path.DirectorySeparatorChar;
        }

        return directory;
    }

    /// <summary>
    /// 把 BMP/JPEG 等完整帧压成真正的 JPEG。已是较小 JPEG 时直接落盘。
    /// </summary>
    public static SaveResult Save(byte[] frame, string filePath, int quality)
    {
        if (frame.Length < 100)
        {
            return SaveResult.Fail("帧太短");
        }

        quality = ClampQuality(quality);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        try
        {
            if (frame.Length >= 2 && frame[0] == 0xFF && frame[1] == 0xD8 && frame.Length < 1_500_000)
            {
                File.WriteAllBytes(filePath, frame);
                return SaveResult.Ok(filePath, new FileInfo(filePath).Length);
            }

            using var image = Image.Load(frame);
            var encoder = new JpegEncoder { Quality = quality };
            image.Save(filePath, encoder);
            var size = new FileInfo(filePath).Length;
            return SaveResult.Ok(filePath, size);
        }
        catch (Exception ex)
        {
            return SaveResult.Fail(ex.Message);
        }
    }
}

public readonly record struct SaveResult(bool Success, string Path, long Bytes, string Error)
{
    public static SaveResult Ok(string path, long bytes) => new(true, path, bytes, string.Empty);

    public static SaveResult Fail(string error) => new(false, string.Empty, 0, error);
}

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

        if (!ImagePayload.TryEncodeJpeg(frame, quality, out var jpeg, out var error))
        {
            return SaveResult.Fail(error);
        }

        try
        {
            File.WriteAllBytes(filePath, jpeg);
            return SaveResult.Ok(filePath, jpeg.Length);
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

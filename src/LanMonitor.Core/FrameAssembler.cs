namespace LanMonitor.Core;

/// <summary>
/// 拼包：兼容易语言被控常见的 JPEG(FFD8…FFD9) 与 BMP(BM+文件长度)。
/// </summary>
public sealed class FrameAssembler
{
    public const int MaxBufferBytes = 12_000_000;

    private readonly List<byte> _buffer = new(256 * 1024);

    public int BufferedBytes => _buffer.Count;

    public void Clear() => _buffer.Clear();

    public IReadOnlyList<byte[]> Push(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return Array.Empty<byte[]>();
        }

        _buffer.AddRange(data.ToArray());
        if (_buffer.Count > MaxBufferBytes)
        {
            _buffer.Clear();
            return Array.Empty<byte[]>();
        }

        var frames = new List<byte[]>();
        while (TryExtractOne(out var frame))
        {
            frames.Add(frame);
        }

        return frames;
    }

    private bool TryExtractOne(out byte[] frame)
    {
        frame = Array.Empty<byte>();
        if (_buffer.Count < 4)
        {
            return false;
        }

        var jpegStart = IndexOfMarker(0xFF, 0xD8, 0);
        if (jpegStart >= 0)
        {
            if (jpegStart > 0)
            {
                _buffer.RemoveRange(0, jpegStart);
            }

            var jpegEnd = IndexOfMarker(0xFF, 0xD9, 2);
            if (jpegEnd < 0)
            {
                return false;
            }

            var length = jpegEnd + 2;
            frame = _buffer.GetRange(0, length).ToArray();
            _buffer.RemoveRange(0, length);
            return frame.Length >= 100;
        }

        if (_buffer.Count >= 6 && _buffer[0] == (byte)'B' && _buffer[1] == (byte)'M')
        {
            var fileSize = BitConverter.ToInt32(new[] { _buffer[2], _buffer[3], _buffer[4], _buffer[5] }, 0);
            if (fileSize < 54 || fileSize > MaxBufferBytes)
            {
                _buffer.RemoveAt(0);
                return TryExtractOne(out frame);
            }

            if (_buffer.Count < fileSize)
            {
                return false;
            }

            frame = _buffer.GetRange(0, fileSize).ToArray();
            _buffer.RemoveRange(0, fileSize);
            return true;
        }

        if (_buffer.Count > 65_536)
        {
            _buffer.Clear();
        }

        return false;
    }

    private int IndexOfMarker(byte a, byte b, int start)
    {
        for (var i = start; i < _buffer.Count - 1; i++)
        {
            if (_buffer[i] == a && _buffer[i + 1] == b)
            {
                return i;
            }
        }

        return -1;
    }
}

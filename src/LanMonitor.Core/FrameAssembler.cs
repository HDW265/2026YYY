namespace LanMonitor.Core;

/// <summary>
/// 拼包顺序：JPEG头 / BMP头 / 4字节长度前缀。
/// 禁止在整段缓冲里搜 FFD8，避免把 BMP 像素误当成 JPEG。
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

        if (_buffer[0] == 0xFF && _buffer[1] == 0xD8)
        {
            return TryExtractJpeg(out frame);
        }

        if (_buffer[0] == (byte)'B' && _buffer[1] == (byte)'M')
        {
            return TryExtractBmp(0, out frame);
        }

        if (TryExtractLengthPrefixed(out frame))
        {
            return true;
        }

        var start = FindSignatureInPrefix(64);
        if (start > 0)
        {
            _buffer.RemoveRange(0, start);
            return TryExtractOne(out frame);
        }

        return false;
    }

    private bool TryExtractJpeg(out byte[] frame)
    {
        frame = Array.Empty<byte>();
        var end = IndexOfMarker(0xFF, 0xD9, 2);
        if (end < 0)
        {
            return false;
        }

        var length = end + 2;
        if (length < 100)
        {
            return false;
        }

        frame = _buffer.GetRange(0, length).ToArray();
        _buffer.RemoveRange(0, length);
        return true;
    }

    private bool TryExtractBmp(int offset, out byte[] frame)
    {
        frame = Array.Empty<byte>();
        if (_buffer.Count < offset + 6)
        {
            return false;
        }

        var fileSize = BitConverter.ToInt32(new[]
        {
            _buffer[offset + 2], _buffer[offset + 3], _buffer[offset + 4], _buffer[offset + 5]
        }, 0);
        if (fileSize < 54 || fileSize > MaxBufferBytes)
        {
            return false;
        }

        if (_buffer.Count < offset + fileSize)
        {
            return false;
        }

        if (offset > 0)
        {
            _buffer.RemoveRange(0, offset);
        }

        frame = _buffer.GetRange(0, fileSize).ToArray();
        _buffer.RemoveRange(0, fileSize);
        return true;
    }

    private bool TryExtractLengthPrefixed(out byte[] frame)
    {
        frame = Array.Empty<byte>();
        var payloadSize = BitConverter.ToInt32(new[] { _buffer[0], _buffer[1], _buffer[2], _buffer[3] }, 0);
        if (payloadSize < 54 || payloadSize > MaxBufferBytes)
        {
            return false;
        }

        if (_buffer.Count < 4 + payloadSize)
        {
            return false;
        }

        var payload = _buffer.GetRange(4, payloadSize).ToArray();
        if (!ImagePayload.LooksLikeImage(payload, 0) && ImagePayload.FindImageStart(payload, 64) < 0)
        {
            return false;
        }

        _buffer.RemoveRange(0, 4 + payloadSize);
        frame = payload;
        return true;
    }

    private int FindSignatureInPrefix(int maxScan)
    {
        var limit = Math.Min(_buffer.Count - 2, maxScan);
        for (var i = 1; i <= limit; i++)
        {
            if (_buffer[i] == 0xFF && _buffer[i + 1] == 0xD8)
            {
                return i;
            }

            if (_buffer[i] == (byte)'B' && _buffer[i + 1] == (byte)'M')
            {
                return i;
            }

            if (i + 4 <= _buffer.Count &&
                _buffer[i] == 40 && _buffer[i + 1] == 0 && _buffer[i + 2] == 0 && _buffer[i + 3] == 0)
            {
                return i;
            }
        }

        return -1;
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

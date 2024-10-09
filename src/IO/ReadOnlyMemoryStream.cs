public class ReadOnlyMemoryStream : Stream
{
    private readonly ReadOnlyMemory<byte> _memory;
    private int _position;

    public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
        _position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _memory.Length;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > _memory.Length)
                throw new ArgumentOutOfRangeException(nameof(value));
            _position = (int)value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesAvailable = _memory.Length - _position;
        int bytesToRead = Math.Min(count, bytesAvailable);
        _memory.Slice(_position, bytesToRead).Span.CopyTo(buffer.AsSpan(offset, bytesToRead));
        _position += bytesToRead;
        return bytesToRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _memory.Length + offset,
            _ => throw new ArgumentException("Invalid seek origin", nameof(origin))
        };

        if (newPosition < 0 || newPosition > _memory.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        _position = (int)newPosition;
        return _position;
    }

    public override void Flush() { }

    public override void SetLength(long value) => 
        throw new NotSupportedException("Cannot set length of ReadOnlyMemoryStream");

    public override void Write(byte[] buffer, int offset, int count) => 
        throw new NotSupportedException("Cannot write to ReadOnlyMemoryStream");
}
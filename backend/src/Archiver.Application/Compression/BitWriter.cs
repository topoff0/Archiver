namespace Archiver.Application.Compression;

internal sealed class BitWriter
{
    private readonly MemoryStream _stream = new();
    private byte _currentByte;
    private int _bitCount;

    public ulong BitLength { get; private set; }

    public void Write(ulong code, int length)
    {
        for (var i = length - 1; i >= 0; i--)
        {
            var bit = (byte)((code >> i) & 1UL);
            _currentByte = (byte)((_currentByte << 1) | bit);
            _bitCount++;
            BitLength++;

            if (_bitCount == 8)
            {
                _stream.WriteByte(_currentByte);
                _currentByte = 0;
                _bitCount = 0;
            }
        }
    }

    public byte[] ToArray()
    {
        if (_bitCount > 0)
        {
            _currentByte <<= 8 - _bitCount;
            _stream.WriteByte(_currentByte);
            _currentByte = 0;
            _bitCount = 0;
        }

        return _stream.ToArray();
    }
}

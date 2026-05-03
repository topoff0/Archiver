namespace Archiver.Application.Compression;

internal sealed class HuffmanNode
{
    public HuffmanNode(byte symbol, ulong frequency)
    {
        Symbol = symbol;
        Frequency = frequency;
    }

    public HuffmanNode(HuffmanNode left, HuffmanNode right)
    {
        Left = left;
        Right = right;
        Frequency = left.Frequency + right.Frequency;
    }

    public byte? Symbol { get; }
    public ulong Frequency { get; }
    public HuffmanNode? Left { get; }
    public HuffmanNode? Right { get; }
    public bool IsLeaf => Symbol.HasValue;
}

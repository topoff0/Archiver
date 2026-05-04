using System.Text;
using Archiver.Application.Abstractions;
using Archiver.Application.Compression;
using Archiver.Application.Services;

var tests = new (string Name, Action Test)[]
{
    ("compress/decompress text roundtrip", CompressTextRoundtrip),
    ("compress/decompress all byte values", CompressAllByteValuesRoundtrip),
    ("compress/decompress single-symbol file", CompressSingleSymbolRoundtrip),
    ("honors selected maximum code length", HonorsSelectedMaxCodeLength),
    ("rejects too-small maximum code length", RejectsTooSmallMaxCodeLength),
    ("password-protected archive roundtrip", PasswordProtectedRoundtrip),
    ("wrong password is rejected", WrongPasswordIsRejected),
    ("corrupted archive format is rejected", CorruptedArchiveIsRejected),
    ("service rejects empty files", ServiceRejectsEmptyFiles),
    ("service rejects invalid maximum code length", ServiceRejectsInvalidMaxCodeLength),
};

var failed = 0;

foreach (var test in tests)
{
    try
    {
        test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} test(s) failed.");
    Environment.Exit(1);
}

Console.WriteLine($"{tests.Length} test(s) passed.");

static void CompressTextRoundtrip()
{
    var input = Input("sample.txt", Encoding.UTF8.GetBytes("Huffman compression test data: aaaabbbccd 123123123"));
    var result = Codec().Compress(input, maxCodeLength: 16, password: null);
    var restored = Codec().Decompress(Input(result.FileName, result.Content), password: null);

    AssertBytesEqual(input.Content, restored.Content);
    AssertValueEqual("sample.txt.huff", result.FileName);
    AssertValueEqual("sample.txt", restored.FileName);
    AssertValueEqual(input.Content.Length, result.OriginalSize);
}

static void CompressAllByteValuesRoundtrip()
{
    var data = Enumerable.Range(0, 256)
        .SelectMany(value => Enumerable.Repeat((byte)value, (value % 7) + 1))
        .ToArray();

    var result = Codec().Compress(Input("bytes.bin", data), maxCodeLength: 12, password: null);
    var restored = Codec().Decompress(Input(result.FileName, result.Content), password: null);

    AssertBytesEqual(data, restored.Content);
}

static void CompressSingleSymbolRoundtrip()
{
    var data = Enumerable.Repeat((byte)42, 4096).ToArray();
    var result = Codec().Compress(Input("single.bin", data), maxCodeLength: 1, password: null);
    var restored = Codec().Decompress(Input(result.FileName, result.Content), password: null);

    AssertBytesEqual(data, restored.Content);
    AssertValueEqual(1, result.MaxCodeLength);
}

static void HonorsSelectedMaxCodeLength()
{
    var data = Encoding.UTF8.GetBytes("aaaaabbbbcccdde");
    var result = Service().Compress(Input("limited.txt", data), maxCodeLength: 8, password: null);
    var restored = Service().Decompress(Input(result.FileName, result.Content), password: null);

    AssertValueEqual(8, result.MaxCodeLength);
    AssertBytesEqual(data, restored.Content);
}

static void RejectsTooSmallMaxCodeLength()
{
    var data = new byte[] { 1, 2, 3, 4, 5 };

    AssertThrows<ArchiveValidationException>(() =>
        Service().Compress(Input("too-small.bin", data), maxCodeLength: 2, password: null));
}

static void PasswordProtectedRoundtrip()
{
    var data = Encoding.UTF8.GetBytes("secret payload secret payload secret payload");
    var result = Service().Compress(Input("secret.txt", data), maxCodeLength: 16, password: "pass-123");
    var restored = Service().Decompress(Input(result.FileName, result.Content), password: "pass-123");

    AssertTrue(result.PasswordProtected, "Archive should be marked as password-protected.");
    AssertBytesEqual(data, restored.Content);
}

static void WrongPasswordIsRejected()
{
    var data = Encoding.UTF8.GetBytes("protected data");
    var result = Service().Compress(Input("protected.txt", data), maxCodeLength: 16, password: "correct");

    AssertThrows<ArchiveValidationException>(() =>
        Service().Decompress(Input(result.FileName, result.Content), password: "wrong"));
}

static void CorruptedArchiveIsRejected()
{
    var invalidArchive = Encoding.ASCII.GetBytes("not-a-huff-archive");

    AssertThrows<ArchiveValidationException>(() =>
        Codec().Decompress(Input("bad.huff", invalidArchive), password: null));
}

static void ServiceRejectsEmptyFiles()
{
    AssertThrows<ArchiveValidationException>(() =>
        Service().Compress(Input("empty.txt", Array.Empty<byte>()), maxCodeLength: 16, password: null));
}

static void ServiceRejectsInvalidMaxCodeLength()
{
    var data = Encoding.UTF8.GetBytes("data");

    AssertThrows<ArchiveValidationException>(() =>
        Service().Compress(Input("invalid.txt", data), maxCodeLength: 33, password: null));
}

static HuffmanArchiveCodec Codec()
{
    return new HuffmanArchiveCodec();
}

static ArchiveService Service()
{
    return new ArchiveService(Codec());
}

static ArchiveInput Input(string fileName, byte[] content)
{
    return new ArchiveInput(content, fileName);
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static void AssertBytesEqual(byte[] expected, byte[] actual)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException($"Byte arrays differ. Expected {expected.Length} bytes, actual {actual.Length} bytes.");
    }
}

static void AssertValueEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

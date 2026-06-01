using System.Buffers.Binary;

namespace Gamestack.Tests.Support;

/// <summary>Builds minimal valid image headers so dimension parsing can be tested without real files.</summary>
public static class TestImages
{
    /// <summary>A minimal PNG header (signature + IHDR) carrying the given dimensions.</summary>
    public static byte[] Png(int width, int height)
    {
        var b = new byte[24];
        ReadOnlySpan<byte> sig = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        sig.CopyTo(b);
        b[11] = 13; // IHDR length
        b[12] = (byte)'I'; b[13] = (byte)'H'; b[14] = (byte)'D'; b[15] = (byte)'R';
        BinaryPrimitives.WriteInt32BigEndian(b.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(b.AsSpan(20, 4), height);
        return b;
    }

    /// <summary>Write a synthetic PNG to <paramref name="path"/>.</summary>
    public static void WritePng(string path, int width, int height)
        => File.WriteAllBytes(path, Png(width, height));
}

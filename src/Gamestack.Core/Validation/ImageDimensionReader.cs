using System.Buffers.Binary;

namespace Gamestack.Core.Validation;

/// <summary>
/// Reads pixel dimensions of common raster formats (PNG, JPEG, BMP, WEBP) directly from their
/// headers — no image decoding, no third-party dependency. Returns <c>null</c> for unrecognized
/// or malformed input so callers can simply skip such files.
/// </summary>
public static class ImageDimensionReader
{
    /// <summary>Try to read <paramref name="width"/> and <paramref name="height"/> from a file.</summary>
    public static bool TryReadDimensions(string path, out int width, out int height)
    {
        width = height = 0;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return TryReadDimensions(fs, out width, out height);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>Try to read dimensions from a seekable stream positioned at the start.</summary>
    public static bool TryReadDimensions(Stream stream, out int width, out int height)
    {
        width = height = 0;
        Span<byte> head = stackalloc byte[32];
        int read = ReadAtLeast(stream, head, head.Length);
        if (read < 12)
            return false;

        // PNG: 89 'P' 'N' 'G' 0D 0A 1A 0A, then IHDR with width@16, height@20 (big-endian).
        if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
        {
            width = BinaryPrimitives.ReadInt32BigEndian(head.Slice(16, 4));
            height = BinaryPrimitives.ReadInt32BigEndian(head.Slice(20, 4));
            return width > 0 && height > 0;
        }

        // BMP: 'B' 'M', BITMAPINFOHEADER width@18 / height@22 (little-endian, height may be negative).
        if (head[0] == 0x42 && head[1] == 0x4D)
        {
            width = BinaryPrimitives.ReadInt32LittleEndian(head.Slice(18, 4));
            height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(head.Slice(22, 4)));
            return width > 0 && height > 0;
        }

        // WEBP: 'RIFF' .... 'WEBP'.
        if (head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46 &&
            head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50)
        {
            return TryReadWebp(head, out width, out height);
        }

        // JPEG: starts FF D8 — scan the segment markers for a Start-Of-Frame.
        if (head[0] == 0xFF && head[1] == 0xD8)
        {
            stream.Seek(2, SeekOrigin.Begin);
            return TryReadJpeg(stream, out width, out height);
        }

        return false;
    }

    private static bool TryReadWebp(ReadOnlySpan<byte> head, out int width, out int height)
    {
        width = height = 0;
        // Chunk FourCC at offset 12.
        var fourCc = head.Slice(12, 4);

        if (fourCc.SequenceEqual("VP8 "u8)) // lossy: keyframe, start code 9D 01 2A at offset 23.
        {
            if (head[23] == 0x9D && head[24] == 0x01 && head[25] == 0x2A)
            {
                width = (head[26] | (head[27] << 8)) & 0x3FFF;
                height = (head[28] | (head[29] << 8)) & 0x3FFF;
                return width > 0 && height > 0;
            }
            return false;
        }

        if (fourCc.SequenceEqual("VP8L"u8)) // lossless: signature 0x2F at offset 20, then packed 14+14 bits.
        {
            if (head[20] != 0x2F)
                return false;
            int b0 = head[21], b1 = head[22], b2 = head[23], b3 = head[24];
            width = ((b1 & 0x3F) << 8 | b0) + 1;
            height = ((b3 & 0x0F) << 10 | b2 << 2 | (b1 & 0xC0) >> 6) + 1;
            return true;
        }

        if (fourCc.SequenceEqual("VP8X"u8)) // extended: 24-bit canvas width-1 @24, height-1 @27 (little-endian).
        {
            width = (head[24] | head[25] << 8 | head[26] << 16) + 1;
            height = (head[27] | head[28] << 8 | head[29] << 16) + 1;
            return true;
        }

        return false;
    }

    private static bool TryReadJpeg(Stream stream, out int width, out int height)
    {
        width = height = 0;
        Span<byte> two = stackalloc byte[2];

        while (true)
        {
            // Advance to the next marker (0xFF, then a non-0xFF, non-0x00 marker byte).
            int b = stream.ReadByte();
            if (b < 0) return false;
            if (b != 0xFF) continue;

            int marker;
            do { marker = stream.ReadByte(); } while (marker == 0xFF);
            if (marker < 0) return false;

            // Standalone markers without a length payload.
            if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
                continue;

            if (ReadAtLeast(stream, two, 2) < 2) return false;
            int length = (two[0] << 8) | two[1];
            if (length < 2) return false;

            bool isSof = marker is 0xC0 or 0xC1 or 0xC2 or 0xC3
                              or 0xC5 or 0xC6 or 0xC7
                              or 0xC9 or 0xCA or 0xCB
                              or 0xCD or 0xCE or 0xCF;
            if (isSof)
            {
                Span<byte> sof = stackalloc byte[5]; // precision(1) + height(2) + width(2)
                if (ReadAtLeast(stream, sof, 5) < 5) return false;
                height = (sof[1] << 8) | sof[2];
                width = (sof[3] << 8) | sof[4];
                return width > 0 && height > 0;
            }

            // Skip this segment's payload.
            stream.Seek(length - 2, SeekOrigin.Current);
        }
    }

    private static int ReadAtLeast(Stream stream, Span<byte> buffer, int min)
    {
        int total = 0;
        while (total < min)
        {
            int n = stream.Read(buffer.Slice(total));
            if (n == 0) break;
            total += n;
        }
        return total;
    }
}

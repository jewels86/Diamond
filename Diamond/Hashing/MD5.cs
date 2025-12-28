using static Diamond.CryptographicOperations;

namespace Diamond;

public static partial class Hashing
{
    #region MD5
    public static byte[] MD5(byte[] data)
    {
        var padded = PadMD5(data);
        int nChunks = padded.Length / 64;
        var hv = InitialHashValuesMD5();

        for (int i = 0; i < nChunks; i++)
        {
            var chunk = new byte[64];
            Copy(padded, i * 64, chunk, 0, 64);
            ProcessChunkMD5(chunk, hv);
        }
    
        // output in little endian
        byte[] hash = new byte[16];
        for (int i = 0; i < 4; i++)
        {
            hash[i * 4] = (byte)hv[i];
            hash[i * 4 + 1] = (byte)(hv[i] >> 8);
            hash[i * 4 + 2] = (byte)(hv[i] >> 16);
            hash[i * 4 + 3] = (byte)(hv[i] >> 24);
        }
        return hash;
    }
    public static byte[] MD5(string data) => MD5(FromString(data));
    public static string MD5Hex(byte[] data) => HexString(MD5(data));
    public static string MD5Hex(string data) => MD5Hex(FromString(data));
    #endregion
    
    #region Helpers
    private static byte[] PadMD5(byte[] data)
    {
        var originalLength = data.Length;
        var originalBitLength = originalLength * 8L;
        var paddedLength = originalLength + 9;
    
        if (paddedLength % 64 != 0) paddedLength += 64 - paddedLength % 64;
        var padded = new byte[paddedLength];
    
        Copy(data, 0, padded, 0, originalLength);
        padded[originalLength] = 0x80;

        // little endian
        for (int i = 0; i < 8; i++)
            padded[paddedLength - 8 + i] = (byte)(originalBitLength >> (i * 8));
        return padded;
    }
    
    private static uint F(uint x, uint y, uint z) => (x & y) | (~x & z);
    private static uint G(uint x, uint y, uint z) => (x & z) | (y & ~z);
    private static uint H(uint x, uint y, uint z) => x ^ y ^ z;
    private static uint I(uint x, uint y, uint z) => y ^ (x | ~z);

    private static void ProcessChunkMD5(byte[] chunk, uint[] hv)
    {
        var m = new uint[16];
        
        for (int i = 0; i < 16; i++) // break into 16 32-bit little-endian words
            m[i] = chunk[i * 4]
                   | (uint)chunk[i * 4 + 1] << 8
                   | (uint)chunk[i * 4 + 2] << 16
                   | (uint)chunk[i * 4 + 3] << 24;
        
        var (a, b, c, d) = (hv[0], hv[1], hv[2], hv[3]);
        var k = RoundConstantsMD5();
        var messageIndex = MessageIndexPatternsMD5();
        var shifts = ShiftsMD5();
        
        for (int i = 0; i < 64; i++)
        {
            uint f;
            if (i < 16) f = F(b, c, d);
            else if (i < 32) f = G(b, c, d);
            else if (i < 48) f = H(b, c, d);
            else f = I(b, c, d);

            f = f + a + k[i] + m[messageIndex[i]];
            (a, d, c, b) = (d, c, b, b + LeftRotate(f, shifts[i]));
        }
    
        hv[0] += a; hv[1] += b; hv[2] += c; hv[3] += d;
    }
    #endregion
    #region Constants
    private static int[] MessageIndexPatternsMD5() =>
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, // rounds 0-15, F
        1, 6, 11, 0, 5, 10, 15, 4, 9, 14, 3, 8, 13, 2, 7, 12, // rounds 16-31, G
        5, 8, 11, 14, 1, 4, 7, 10, 13, 0, 3, 6, 9, 12, 15, 2, // rounds 32-47, H
        0, 7, 14, 5, 12, 3, 10, 1, 8, 15, 6, 13, 4, 11, 2, 9 // rounds 48-63, I
    ];

    private static int[] ShiftsMD5() =>
    [
        7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, // rounds 0-15
        5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, // rounds 16-31
        4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, // rounds 32-47
        6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21  // rounds 48-63
    ];
    
    private static uint[] InitialHashValuesMD5() =>
    [
        0x67452301,
        0xefcdab89,
        0x98badcfe,
        0x10325476
    ];

    private static uint[] RoundConstantsMD5() =>
    [
        0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
        0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
        0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
        0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
        0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
        0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
        0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
        0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
        0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
        0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
        0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
        0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
        0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
        0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
        0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
        0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
    ];
    #endregion
}
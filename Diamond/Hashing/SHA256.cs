using static Diamond.CryptographicOperations;

namespace Diamond;

public static partial class Hashing
{
    #region SHA256 Implementation
    public static byte[] SHA256Family(byte[] data, uint[] hv, int outputBytes)
    {
        var padded = PadSHA256(data);
        int nChunks = padded.Length / 64;

        for (int i = 0; i < nChunks; i++)
        {
            var chunk = new byte[64];
            Copy(padded, i * 64, chunk, 0, 64);
            ProcessChunk(chunk, hv);
        }
        
        byte[] hash = new byte[32];
        for (int i = 0; i < 8; i++)
        {
            hash[i * 4] = (byte)(hv[i] >> 24);
            hash[i * 4 + 1] = (byte)(hv[i] >> 16);
            hash[i * 4 + 2] = (byte)(hv[i] >> 8);
            hash[i * 4 + 3] = (byte)hv[i];
        }
        return hash.Take(outputBytes).ToArray();
    }
    #endregion
    
    #region SHA256
    public static byte[] SHA256(byte[] data) => SHA256Family(data, InitialHashValuesSHA256(), 32);
    public static byte[] SHA256(string data) => SHA256(FromString(data));
    public static string SHA256Hex(byte[] data) => HexString(SHA256(data));
    public static string SHA256Hex(string data) => SHA256Hex(FromString(data));
    #endregion
    #region SHA224
    public static byte[] SHA224(byte[] data) => SHA256Family(data, InitialHashValuesSHA224(), 28);
    public static byte[] SHA224(string data) => SHA224(FromString(data));
    public static string SHA224Hex(byte[] data) => HexString(SHA224(data));
    public static string SHA224Hex(string data) => SHA224Hex(FromString(data));
    #endregion
    
    #region Helpers
    private static byte[] PadSHA256(byte[] data)
    {
        var originalLength = data.Length;
        var originalBitLength = originalLength * 8L;
        var paddedLength = originalLength + 9; // we need 1 byte for the '1' bit, and 8 bytes for the length at the end, so this is the minimum length
        
        if (paddedLength % 64 != 0) paddedLength += 64 - paddedLength % 64; // pad to the next multiple of 64 bytes
        var padded = new byte[paddedLength];
        
        Copy(data, 0, padded, 0, originalLength);
        padded[originalLength] = 0x80; // '1' bit (0x80 = 1000 0000)

        for (int i = 0; i < 8; i++)
            padded[paddedLength - 8 + i] = (byte)(originalBitLength >> (56 - i * 8)); // this is big-endian, so we want to shift by 56 first, then 48, and so on
        return padded;
    }
    private static void ProcessChunk(byte[] chunk, uint[] hv)
    {
        var w = new uint[64];
        
        for (int i = 0; i < 16; i++) // break it into 16 32-bit big-endian words
            w[i] = (uint)chunk[i * 4] << 24
                   | (uint)chunk[i * 4 + 1] << 16
                   | (uint)chunk[i * 4 + 2] << 8
                   | chunk[i * 4 + 3];

        for (int i = 16; i < 64; i++) // extend to 64 words 
        {
            var s0 = LSigma0(w[i - 15]);
            var s1 = LSigma1(w[i - 2]);
            w[i] = w[i - 16] + s0 + w[i - 7] + s1;
        }

        var (a, b, c, d) = (hv[0], hv[1], hv[2], hv[3]);
        var (e, f, g, h) = (hv[4], hv[5], hv[6], hv[7]);
        var k = RoundConstantsSHA256();

        for (int i = 0; i < 64; i++) // 64 rounds
        {
            var us1 = USigma1(e);
            var ch = Choose(e, f, g);
            var temp1 = h + us1 + ch + k[i] + w[i];
            var us0 = USigma0(a);
            var maj = Majority(a, b, c);
            var temp2 = us0 + maj;
            (h, g, f, e) = (g, f, e, d + temp1);
            (d, c, b, a) = (c, b, a, temp1 + temp2);
        }
        
        // add the new compressed chunk to the hash Values
        hv[0] += a; hv[1] += b; hv[2] += c; hv[3] += d;
        hv[4] += e; hv[5] += f; hv[6] += g; hv[7] += h;
    }
    #endregion
    #region Constants
    private static uint[] InitialHashValuesSHA256() =>
    [
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
        0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
    ];

    private static uint[] InitialHashValuesSHA224() =>
    [
        0xc1059ed8, 0x367cd507, 0x3070dd17, 0xf70e5939,
        0xffc00b31, 0x68581511, 0x64f98fa7, 0xbefa4fa4
    ];

    private static uint[] RoundConstantsSHA256() =>
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
        0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
        0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
        0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
        0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
        0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
        0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
        0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
        0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    ];
    #endregion
}
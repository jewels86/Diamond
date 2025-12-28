using System.Runtime.InteropServices.ObjectiveC;
using System.Text;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Jewels.Lazulite;
using static Diamond.CryptographicOperations;

namespace Diamond;

public static partial class Hashing
{
    #region SHA512 Implementation
    public static byte[] SHA512Family(byte[] data, ulong[] hv, int outputBytes)
    {
        var padded = PadSHA512(data);
        int nChunks = padded.Length / 128;

        for (int i = 0; i < nChunks; i++)
        {
            var chunk = new byte[128];
            Copy(padded, i * 128, chunk, 0, 128);
            ProcessChunk(chunk, hv);
        }
    
        byte[] hash = new byte[64];
        for (int i = 0; i < 8; i++)
        {
            hash[i * 8] = (byte)(hv[i] >> 56);
            hash[i * 8 + 1] = (byte)(hv[i] >> 48);
            hash[i * 8 + 2] = (byte)(hv[i] >> 40);
            hash[i * 8 + 3] = (byte)(hv[i] >> 32);
            hash[i * 8 + 4] = (byte)(hv[i] >> 24);
            hash[i * 8 + 5] = (byte)(hv[i] >> 16);
            hash[i * 8 + 6] = (byte)(hv[i] >> 8);
            hash[i * 8 + 7] = (byte)hv[i];
        }
        return hash.Take(outputBytes).ToArray();
    }
    #endregion
    
    #region SHA512
    public static byte[] SHA512(byte[] data) => SHA512Family(data, InitialHashValuesSHA512(), 64);
    public static byte[] SHA512(string data) => SHA512(FromString(data));
    public static string SHA512Hex(byte[] data) => HexString(SHA512(data));
    public static string SHA512Hex(string data) => SHA512Hex(FromString(data));
    #endregion
    #region SHA384
    public static byte[] SHA384(byte[] data) => SHA512Family(data, InitialHashValuesSHA384(), 48);
    public static byte[] SHA384(string data) => SHA384(FromString(data));
    public static string SHA384Hex(byte[] data) => HexString(SHA384(data));
    public static string SHA384Hex(string data) => SHA384Hex(FromString(data));
    #endregion
    #region SHA512_224
    public static byte[] SHA512_224(byte[] data) => SHA512Family(data, InitialHashValuesSHA512_224(), 28);
    public static byte[] SHA512_224(string data) => SHA512_224(FromString(data));
    public static string SHA512_224Hex(byte[] data) => HexString(SHA512_224(data));
    public static string SHA512_224Hex(string data) => SHA512_224Hex(FromString(data));
    #endregion
    #region SHA512_256
    public static byte[] SHA512_256(byte[] data) => SHA512Family(data, InitialHashValuesSHA512_256(), 32);
    public static byte[] SHA512_256(string data) => SHA512_256(FromString(data));
    public static string SHA512_256Hex(byte[] data) => HexString(SHA512_256(data));
    public static string SHA512_256Hex(string data) => SHA512_256Hex(FromString(data));
    #endregion
    
    #region Helpers
    private static byte[] PadSHA512(byte[] data)
    {
        var originalLength = data.Length;
        var originalBitLength = (ulong)originalLength * 8UL;
        var paddedLength = originalLength + 17; // we need 1 byte for the '1' bit, and 16 bytes for the length at the end
    
        if (paddedLength % 128 != 0) paddedLength += 128 - paddedLength % 128; // pad to the next multiple of 128 bytes
        var padded = new byte[paddedLength];
    
        Copy(data, 0, padded, 0, originalLength);
        padded[originalLength] = 0x80; // '1' bit (0x80 = 1000 0000)

        // unless you have a lot of data, high bits are 0
        for (int i = 0; i < 8; i++)
            padded[paddedLength - 16 + i] = 0; // high 64 bits (always 0 for reasonable message sizes)
    
        for (int i = 0; i < 8; i++)
            padded[paddedLength - 8 + i] = (byte)(originalBitLength >> (56 - i * 8)); // low 64 bits
    
        return padded;
    }

    private static void ProcessChunk(byte[] chunk, ulong[] hv)
    {
        var w = new ulong[80]; 
    
        for (int i = 0; i < 16; i++) // break it into 16 64-bit big-endian words
            w[i] = (ulong)chunk[i * 8] << 56
                   | (ulong)chunk[i * 8 + 1] << 48
                   | (ulong)chunk[i * 8 + 2] << 40
                   | (ulong)chunk[i * 8 + 3] << 32
                   | (ulong)chunk[i * 8 + 4] << 24
                   | (ulong)chunk[i * 8 + 5] << 16
                   | (ulong)chunk[i * 8 + 6] << 8
                   | chunk[i * 8 + 7];

        for (int i = 16; i < 80; i++) // extend to 80 words 
        {
            var s0 = LSigma0(w[i - 15]);
            var s1 = LSigma1(w[i - 2]);
            w[i] = w[i - 16] + s0 + w[i - 7] + s1;
        }

        var (a, b, c, d) = (hv[0], hv[1], hv[2], hv[3]);
        var (e, f, g, h) = (hv[4], hv[5], hv[6], hv[7]);
        var k = RoundConstantsSHA512();

        for (int i = 0; i < 80; i++) // 80 rounds
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
    private static ulong[] InitialHashValuesSHA512() =>
    [
        0x6a09e667f3bcc908, 0xbb67ae8584caa73b, 0x3c6ef372fe94f82b, 0xa54ff53a5f1d36f1,
        0x510e527fade682d1, 0x9b05688c2b3e6c1f, 0x1f83d9abfb41bd6b, 0x5be0cd19137e2179
    ];
    private static ulong[] InitialHashValuesSHA384() =>
    [
        0xcbbb9d5dc1059ed8, 0x629a292a367cd507, 0x9159015a3070dd17, 0x152fecd8f70e5939,
        0x67332667ffc00b31, 0x8eb44a8768581511, 0xdb0c2e0d64f98fa7, 0x47b5481dbefa4fa4
    ];
    private static ulong[] InitialHashValuesSHA512_224() =>
    [
        0x8c3d37c819544da2, 0x73e1996689dcd4d6, 0x1dfab7ae32ff9c82, 0x679dd514582f9fcf,
        0x0f6d2b697bd44da8, 0x77e36f7304c48942, 0x3f9d85a86a1d36c8, 0x1112e6ad91d692a1
    ];
    private static ulong[] InitialHashValuesSHA512_256() =>
    [
        0x22312194fc2bf72c, 0x9f555fa3c84c64c2, 0x2393b86b6f53b151, 0x963877195940eabd,
        0x96283ee2a88effe3, 0xbe5e1e2553863992, 0x2b0199fc2c85b8aa, 0x0eb72ddc81c52ca2
    ];

    private static ulong[] RoundConstantsSHA512() =>
    [
        0x428a2f98d728ae22, 0x7137449123ef65cd, 0xb5c0fbcfec4d3b2f, 0xe9b5dba58189dbbc,
        0x3956c25bf348b538, 0x59f111f1b605d019, 0x923f82a4af194f9b, 0xab1c5ed5da6d8118,
        0xd807aa98a3030242, 0x12835b0145706fbe, 0x243185be4ee4b28c, 0x550c7dc3d5ffb4e2,
        0x72be5d74f27b896f, 0x80deb1fe3b1696b1, 0x9bdc06a725c71235, 0xc19bf174cf692694,
        0xe49b69c19ef14ad2, 0xefbe4786384f25e3, 0x0fc19dc68b8cd5b5, 0x240ca1cc77ac9c65,
        0x2de92c6f592b0275, 0x4a7484aa6ea6e483, 0x5cb0a9dcbd41fbd4, 0x76f988da831153b5,
        0x983e5152ee66dfab, 0xa831c66d2db43210, 0xb00327c898fb213f, 0xbf597fc7beef0ee4,
        0xc6e00bf33da88fc2, 0xd5a79147930aa725, 0x06ca6351e003826f, 0x142929670a0e6e70,
        0x27b70a8546d22ffc, 0x2e1b21385c26c926, 0x4d2c6dfc5ac42aed, 0x53380d139d95b3df,
        0x650a73548baf63de, 0x766a0abb3c77b2a8, 0x81c2c92e47edaee6, 0x92722c851482353b,
        0xa2bfe8a14cf10364, 0xa81a664bbc423001, 0xc24b8b70d0f89791, 0xc76c51a30654be30,
        0xd192e819d6ef5218, 0xd69906245565a910, 0xf40e35855771202a, 0x106aa07032bbd1b8,
        0x19a4c116b8d2d0c8, 0x1e376c085141ab53, 0x2748774cdf8eeb99, 0x34b0bcb5e19b48a8,
        0x391c0cb3c5c95a63, 0x4ed8aa4ae3418acb, 0x5b9cca4f7763e373, 0x682e6ff3d6b2b8a3,
        0x748f82ee5defb2fc, 0x78a5636f43172f60, 0x84c87814a1f0ab72, 0x8cc702081a6439ec,
        0x90befffa23631e28, 0xa4506cebde82bde9, 0xbef9a3f7b2c67915, 0xc67178f2e372532b,
        0xca273eceea26619c, 0xd186b8c721c0c207, 0xeada7dd6cde0eb1e, 0xf57d4f7fee6ed178,
        0x06f067aa72176fba, 0x0a637dc5a2c898a6, 0x113f9804bef90dae, 0x1b710b35131c471b,
        0x28db77f523047d84, 0x32caab7b40c72493, 0x3c9ebe0a15c9bebc, 0x431d67c49c100d4c,
        0x4cc5d4becb3e42b6, 0x597f299cfc657e2a, 0x5fcb6fab3ad6faec, 0x6c44198c4a475817
    ];
    #endregion
}
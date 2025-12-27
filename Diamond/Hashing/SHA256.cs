using System.Runtime.InteropServices.ObjectiveC;
using System.Text;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public static partial class Hashing
{
    public static byte[] SHA256(byte[] data)
    {
        var padded = PadSHA256(data);
        var hv = ComputeInitialHashValuesSHA256();
        int nChunks = padded.Length / 64;

        for (int i = 0; i < nChunks; i++)
        {
            var chunk = new byte[64];
            CryptographicOperations.Copy(padded, i * 64, chunk, 0, 64);
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
        return hash;
    }
    public static byte[] SHA256(string data) => SHA256(Encoding.UTF8.GetBytes(data));
    
    public static string SHA256Hex(byte[] data)
    {
        byte[] hash = SHA256(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
    public static string SHA256Hex(string data) => SHA256Hex(Encoding.UTF8.GetBytes(data));
    #region Helpers
    private static byte[] PadSHA256(byte[] data)
    {
        var originalLength = data.Length;
        var originalBitLength = originalLength * 8L;
        var paddedLength = originalLength + 9; // we need 1 byte for the '1' bit, and 8 bytes for the length at the end, so this is the minimum length
        
        if (paddedLength % 64 != 0) paddedLength += 64 - paddedLength % 64; // pad to the next multiple of 64 bytes
        var padded = new byte[paddedLength];
        
        CryptographicOperations.Copy(data, 0, padded, 0, originalLength);
        padded[originalLength] = 0x80; // '1' bit (0x80 = 1000 0000)

        for (int i = 0; i < 8; i++)
            padded[paddedLength - 8 + i] = (byte)(originalBitLength >> (56 - i * 8)); // this is big-endian, so we want to shift by 56 first, then 48, and so on
        return padded;
    }

    private static uint[] ComputeInitialHashValuesSHA256()
    {
        var primes = CryptographicOperations.PrimesTo8th();
        var hv = new uint[8];

        for (int i = 0; i < 8; i++)
        {
            var squareRoot = XMath.Sqrt(primes[i]);
            var fractional = squareRoot - XMath.Floor(squareRoot);
            hv[i] = (uint)(fractional * 0x1_0000_0000UL); // take the first 32 bits- we're multiplying by 2^32 to shift the fractional part into int range
        }
        
        return hv;
    }
    
    private static uint[] ComputeRoundConstantsSHA256()
    {
        var k = new uint[64];
        var primes = CryptographicOperations.PrimesTo64th();

        for (int i = 0; i < 64; i++)
        {
            var cubeRoot = XMath.Pow(primes[i], 1.0 / 3.0);
            var fractional = cubeRoot - XMath.Floor(cubeRoot);
            k[i] = (uint)(fractional * 0x1_0000_0000UL); // take the first 32 bits
        }
        
        return k;
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
            var s0 = CryptographicOperations.LSigma0(w[i - 15]);
            var s1 = CryptographicOperations.LSigma1(w[i - 2]);
            w[i] = w[i - 16] + s0 + w[i - 7] + s1;
        }

        var (a, b, c, d) = (hv[0], hv[1], hv[2], hv[3]);
        var (e, f, g, h) = (hv[4], hv[5], hv[6], hv[7]);
        var k = ComputeRoundConstantsSHA256();

        for (int i = 0; i < 64; i++) // 64 rounds
        {
            var us1 = CryptographicOperations.USigma1(e);
            var ch = CryptographicOperations.Choose(e, f, g);
            var temp1 = h + us1 + ch + k[i] + w[i];
            var us0 = CryptographicOperations.USigma0(a);
            var maj = CryptographicOperations.Majority(a, b, c);
            var temp2 = us0 + maj;
            (h, g, f, e) = (g, f, e, d + temp1);
            (d, c, b, a) = (c, b, a, temp1 + temp2);
        }
        
        // add the new compressed chunk to the hash Values
        hv[0] += a; hv[1] += b; hv[2] += c; hv[3] += d;
        hv[4] += e; hv[5] += f; hv[6] += g; hv[7] += h;
    }
    #endregion
}
using System.Text;
using ILGPU;
using ILGPU.Runtime;

namespace Diamond;

public static class CryptographicOperations
{
    #region Operations
    public static uint RightRotate(uint x, int n) => (x >> n) | (x << (32 - n));
    public static uint LeftRotate(uint x, int n) => (x << n) | (x >> (32 - n));
    public static uint Choose(uint x, uint y, uint z) => (x & y) ^ (~x & z);
    public static uint Majority(uint x, uint y, uint z) => (x & y) ^ (x & z) ^ (y & z);
    public static uint USigma0(uint x) => RightRotate(x, 2) ^ RightRotate(x, 13) ^ RightRotate(x, 22);
    public static uint USigma1(uint x) => RightRotate(x, 6) ^ RightRotate(x, 11) ^ RightRotate(x, 25);
    public static uint LSigma0(uint x) => RightRotate(x, 7) ^ RightRotate(x, 18) ^ (x >> 3);
    public static uint LSigma1(uint x) => RightRotate(x, 17) ^ RightRotate(x, 19) ^ (x >> 10);

    public static ulong RightRotate(ulong x, int n) => (x >> n) | (x << (64 - n));
    public static ulong Choose(ulong x, ulong y, ulong z) => (x & y) ^ (~x & z);
    public static ulong Majority(ulong x, ulong y, ulong z) => (x & y) ^ (x & z) ^ (y & z);
    public static ulong USigma0(ulong x) => RightRotate(x, 28) ^ RightRotate(x, 34) ^ RightRotate(x, 39);
    public static ulong USigma1(ulong x) => RightRotate(x, 14) ^ RightRotate(x, 18) ^ RightRotate(x, 41);
    public static ulong LSigma0(ulong x) => RightRotate(x, 1) ^ RightRotate(x, 8) ^ (x >> 7);
    public static ulong LSigma1(ulong x) => RightRotate(x, 19) ^ RightRotate(x, 61) ^ (x >> 6);
    
    public static ulong FloatsAsLong(float f1, float f2)
    {
        uint high = Interop.FloatAsInt(f1);
        uint low = Interop.FloatAsInt(f2);
        return ((ulong)high << 32) | low;
    }

    public static (float, float) LongAsFloats(ulong value)
    {
        uint high = (uint)(value >> 32);
        uint low = (uint)value;
        return (Interop.IntAsFloat(high), Interop.IntAsFloat(low));
    }
    #endregion
    #region Kernel Helpers
    public static void Copy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int length)
    {
        for (int i = 0; i < length; i++) destination[destinationIndex + i] = source[sourceIndex + i];
    }

    public static void Copy(ArrayView1D<float, Stride1D.Dense> source, int sourceIndex, ArrayView1D<float, Stride1D.Dense> dest, int destIndex, int length)
    {
        for (int i = 0; i < length; i++) dest[destIndex + i] = source[sourceIndex + i];
    }
    #endregion
    #region String Helpers
    public static byte[] FromString(string str) => Encoding.UTF8.GetBytes(str);
    
    public static string HexString(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLower();
    public static string HexString(uint[] words) => "[" + string.Join(", ", words.Select(word => word.ToString("X8"))) + "]";
    public static string HexString(ulong[] words) => "[" + string.Join(", ", words.Select(word => word.ToString("X16"))) + "]";
    #endregion
    
    #region Constants
    public static int[] PrimesTo80th() => // later replace these with just primes to 100 and people can loop through what they need
    [
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,
        59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131,
        137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223,
        227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311,
        313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409
    ];
    public static int[] PrimesTo64th() => 
    [
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,
        59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131,
        137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223,
        227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311
    ];

    public static int[] PrimesTo8th() => [2, 3, 5, 7, 11, 13, 17, 19];
    #endregion
}
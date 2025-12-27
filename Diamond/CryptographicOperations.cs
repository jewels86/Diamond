namespace Diamond;

public static class CryptographicOperations
{
    #region Operations
    public static uint RightRotate(uint x, int n) => x >> n | x << 32 - n;
    public static uint Choose(uint x, uint y, uint z) => (x & y) ^ (~x & z);
    public static uint Majority(uint x, uint y, uint z) => (x & y) ^ (x & z) ^ (y & z);
    public static uint USigma0(uint x) => RightRotate(x, 2) ^ RightRotate(x, 13) ^ RightRotate(x, 22);
    public static uint USigma1(uint x) => RightRotate(x, 6) ^ RightRotate(x, 11) ^ RightRotate(x, 25);
    public static uint LSigma0(uint x) => RightRotate(x, 7) ^ RightRotate(x, 18) ^ (x >> 3);
    public static uint LSigma1(uint x) => RightRotate(x, 17) ^ RightRotate(x, 19) ^ (x >> 10);
    #endregion
    
    #region Constants
    public static int[] PrimesTo64th() => 
    [
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,
        59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131,
        137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223,
        227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311
    ];
    public static int[] PrimesTo8th() => PrimesTo64th().Take(8).ToArray();
    #endregion
}
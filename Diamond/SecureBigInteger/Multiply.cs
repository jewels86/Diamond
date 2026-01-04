using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator *(SecureBigInteger a, SecureBigInteger b) => Multiply(a, b);
    
    public static SecureBigInteger Multiply(SecureBigInteger a, SecureBigInteger b)
    {
        var resultSize = a.Length + b.Length;
        var result = new uint[resultSize];

        for (int i = 0; i < a.Length; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var carry = 0UL;
            for (int j = 0; j < b.Length; j++)
            {
                var bVal = b.TryGetLimb(j, 0);
                var product = (ulong)aVal * bVal + result[i + j] + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            result[i + b.Length] = (uint)carry;
        }
        
        return result;
    }
}
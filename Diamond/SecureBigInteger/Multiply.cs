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

    public static SecureBigInteger OpMSrT(SecureBigInteger a, SecureBigInteger b, int shiftBits, int resultLimbs)
    {
        // this does an optimized Trim(a * b >> shiftBits, resultLimbs) (multiply, shift right, and trim)
        // this is NOT constant time with respect to shiftBits or resultLimbs!
        // we also assume they are the same size
        
        var result = new uint[resultLimbs];
        var shiftLimbs = shiftBits / 32;
        var shiftRemainder = shiftBits % 32;
        
        for (int i = 0; i < a.Length; i++)
        {
            var aVal = a[i];
            var carry = 0UL;
        
            for (int j = 0; j < b.Length; j++)
            {
                var productLimbIndex = i + j;
            
                if (productLimbIndex < shiftLimbs) continue;
                if (productLimbIndex >= shiftLimbs + resultLimbs) break;

                var bVal = b[j];
                var product = (ulong)aVal * bVal + result[productLimbIndex - shiftLimbs] + carry;
                result[productLimbIndex - shiftLimbs] = (uint)product;
                carry = product >> 32;
            }
        
            var carryIndex = i + b.Length;
            if (carryIndex >= shiftLimbs && carryIndex < shiftLimbs + resultLimbs) 
                result[carryIndex - shiftLimbs] = (uint)carry;
        }

        if (shiftRemainder == 0) return new(result);
        
        var leftShift = 32 - shiftRemainder;
        for (int i = 0; i < resultLimbs - 1; i++) 
            result[i] = result[i] >> shiftRemainder | result[i + 1] << leftShift;
        result[resultLimbs - 1] >>= shiftRemainder;

        return new(result);
    }
}
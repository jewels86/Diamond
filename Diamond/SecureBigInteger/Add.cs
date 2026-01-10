using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator +(SecureBigInteger a, SecureBigInteger b) => Add(a, b);
    public static SecureBigInteger operator -(SecureBigInteger a, SecureBigInteger b) => Subtract(a, b);
    
    public static SecureBigInteger Add(SecureBigInteger a, SecureBigInteger b)
    {
        var maxLen = Math.Max(a.Length, b.Length);
        
        var carry = 0U;
        var result = new uint[maxLen + 1];
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var bVal = b.TryGetLimb(i, 0);
            var sum = (ulong)aVal + bVal + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        result[maxLen] = carry;
        
        return result;
    }

    public static SecureBigInteger Subtract(SecureBigInteger a, SecureBigInteger b)
    {
        var maxLen = Math.Max(a.Length, b.Length);
        
        var borrow = 0U;
        var result = new uint[maxLen];
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var bVal = b.TryGetLimb(i, 0);
            var diff = (ulong)aVal - bVal - borrow;
            result[i] = (uint)diff;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return result;
    }

    public static SecureBigInteger OpAOS(SecureBigInteger a, SecureBigInteger b, uint shouldSubtract)
    {
        // this does an optimized add or subtract depending on shouldSubtract
        
        var mask = CryptographicOperations.ConstantTime.Select(shouldSubtract, 0xFFFFFFFF, 0);
        var maxLen = Math.Max(a.Length, b.Length);
        SecureBigInteger paddedA = a.Length != maxLen ? Pad(a, maxLen) : a;
        SecureBigInteger paddedB = b.Length != maxLen ? Pad(b, maxLen) : b;
        var result = new uint[maxLen + 1];

        var carry = CryptographicOperations.ConstantTime.Select(shouldSubtract, 1UL, 0UL);
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = paddedA[i];
            var bVal = paddedB[i] ^ mask;
            var sum = (ulong)aVal + bVal + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        
        result[maxLen] = (uint)carry;
        return new(result);
    }
}
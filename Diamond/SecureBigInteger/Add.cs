using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator +(SecureBigInteger a, SecureBigInteger b) => Add(a, b);
    public static SecureBigInteger operator -(SecureBigInteger a, SecureBigInteger b) => Subtract(a, b, out _);
    
    #region Standard
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
    
    public static SecureBigInteger Subtract(SecureBigInteger a, SecureBigInteger b, out uint borrowOut)
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
        
        borrowOut = borrow;
        return result;
    }

    public static void AddInto(SecureBigInteger big, int offset, SecureBigInteger other)
    {
        var carry = 0UL;
        for (int i = 0; i < other.Length; i++)
        {
            var sum = (ulong)big[offset + i] + other.TryGetLimb(i, 0) + carry;
            big[offset + i] = (uint)sum;
            carry = sum >> 32;
        }
    
        for (int i = offset + other.Length; i < big.Length; i++)
        {
            var sum = big[i] + carry;
            big[i] = (uint)sum;
            carry = sum >> 32;
        }
    }
    #endregion

    #region Optimized
    public static SecureBigInteger OpAOS(SecureBigInteger a, SecureBigInteger b, uint shouldSubtract)
    {
        // this does an optimized add or subtract depending on shouldSubtract
        
        var mask = CryptographicOperations.ConstantTime.Select(shouldSubtract, 0xFFFFFFFF, 0);
        var maxLen = Math.Max(a.Length, b.Length);
        var result = new uint[maxLen + 1];

        var carry = CryptographicOperations.ConstantTime.Select(shouldSubtract, 1UL, 0UL);
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = i < a.Length ? a[i] : 0;
            var bVal = i < b.Length ? b[i] ^ mask : 0;
            var sum = (ulong)aVal + bVal + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        
        var finalCarry = CryptographicOperations.ConstantTime.Select(shouldSubtract, 0UL, carry);
        result[maxLen] = (uint)finalCarry;
        return new(result);
    }
    
    public static void AddOrSubtractInto(SecureBigInteger big, int offset, SecureBigInteger other, uint shouldSubtract)
    {
        var mask = CryptographicOperations.ConstantTime.Select(shouldSubtract, 0xFFFFFFFF, 0);
        var carry = CryptographicOperations.ConstantTime.Select(shouldSubtract, 1UL, 0UL);

        for (int i = 0; i < other.Length; i++)
        {
            var aVal = big.TryGetLimb(offset + i, 0);
            var bVal = other.TryGetLimb(i, 0) ^ mask;
            var sum = (ulong)aVal + bVal + carry;
            big.TrySetLimb(offset + i, (uint)sum);
            carry = sum >> 32;
        }
    
        for (int i = offset + other.Length; i < big.Length; i++)
        {
            var aVal = big.TryGetLimb(i, 0);
            var sum = aVal + carry;
            big[i] = (uint)sum;
            carry = sum >> 32;
        }
    }
    #endregion
}
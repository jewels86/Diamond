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
            var aVal = a.OpTryGetLimb(i, 0);
            var bVal = b.OpTryGetLimb(i, 0);
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
            var aVal = a.OpTryGetLimb(i, 0);
            var bVal = b.OpTryGetLimb(i, 0);
            var diff = (ulong)aVal - bVal - borrow;
            result[i] = (uint)diff;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        borrowOut = borrow;
        return result;
    }

    public static void AddInto(SecureBigInteger a, int offset, SecureBigInteger b)
    {
        var carry = 0UL;
        for (int i = 0; i < b.Length; i++)
        {
            var sum = (ulong)a[offset + i] + b.OpTryGetLimb(i, 0) + carry;
            a[offset + i] = (uint)sum;
            carry = sum >> 32;
        }
    
        for (int i = offset + b.Length; i < a.Length; i++)
        {
            var sum = a[i] + carry;
            a[i] = (uint)sum;
            carry = sum >> 32;
        }
    }
    
    public static void AddOrSubtractInto(SecureBigInteger a, int offset, SecureBigInteger b, uint shouldSubtract)
    {
        var mask = CryptographicOperations.ConstantTime.Select(shouldSubtract, 0xFFFFFFFF, 0);
        var carry = CryptographicOperations.ConstantTime.Select(shouldSubtract, 1UL, 0UL);

        for (int i = 0; i < b.Length; i++)
        {
            var aVal = a.OpTryGetLimb(offset + i, 0);
            var bVal = b.OpTryGetLimb(i, 0) ^ mask;
            var sum = (ulong)aVal + bVal + carry;
            a.OpTrySetLimb(offset + i, (uint)sum);
            carry = sum >> 32;
        }
    
        for (int i = offset + b.Length; i < a.Length; i++)
        {
            var aVal = a.OpTryGetLimb(i, 0);
            var sum = aVal + carry;
            a[i] = (uint)sum;
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
            var aVal = a.OpTryGetLimb(i, 0);
            var bVal = b.OpTryGetLimb(i, 0) ^ mask;
            var sum = (ulong)aVal + bVal + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        
        var finalCarry = CryptographicOperations.ConstantTime.Select(shouldSubtract, 0UL, carry);
        result[maxLen] = (uint)finalCarry;
        return new(result);
    }
    
    public static SecureBigInteger OpST(SecureBigInteger a, SecureBigInteger b, int resultLimbs)
    {
        var result = new uint[resultLimbs];
        var borrow = 0UL;
    
        for (int i = 0; i < resultLimbs; i++)
        {
            var aVal = a.OpTryGetLimb(i, 0);
            var bVal = b.OpTryGetLimb(i, 0);
            var diff = (ulong)aVal - bVal - borrow;
            result[i] = (uint)diff;
            borrow = (diff >> 32) & 1;
        }
    
        return new(result);
    }
    #endregion
}
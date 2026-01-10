using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator &(SecureBigInteger a, SecureBigInteger b) => BitwiseAnd(a, b);
    public static SecureBigInteger operator |(SecureBigInteger a, SecureBigInteger b) => BitwiseOr(a, b);
    public static SecureBigInteger operator ^(SecureBigInteger a, SecureBigInteger b) => BitwiseXor(a, b);
    public static SecureBigInteger operator ~(SecureBigInteger a) => BitwiseNot(a);
    
    public static uint[] GetBits(SecureBigInteger big)
    {
        var result = new uint[big.BitLength];
        for (int i = 0; i < big.BitLength; i++) 
            result[i] = GetBit(big, i);
        
        return result;
    }

    public static uint GetBit(SecureBigInteger big, int i)
    {
        var limbIndex = i / 32;
        var bitPosition = i % 32;
        var limb = big[limbIndex];
        return limb >> bitPosition & 1u;
    }

    public void ClearBit(int i)
    {
        var limbIndex = i / 32;
        var bitPosition = i % 32;
        _value[limbIndex] &= ~(1u << bitPosition);
    }

    public int LogicalBitLength()
    {
        var bitLength = 0;
        var foundNonZero = 0U;
        
        for (int i = _value.Length - 1; i >= 0; i--)
        {
            var limbIsNonZero = CryptographicOperations.ConstantTime.IsNonZero(_value[i]);
            var limbBits = 32 - CryptographicOperations.ConstantTime.CountLeadingZeros(_value[i]);
            
            var thisLimbBitLength = i * 32 + limbBits;
            
            var shouldUpdate = limbIsNonZero & ~foundNonZero;
            bitLength = (int)CryptographicOperations.ConstantTime.Select(shouldUpdate, (uint)thisLimbBitLength, (uint)bitLength);
            
            foundNonZero |= limbIsNonZero;
        }
        
        return bitLength;
    }
    
    public static SecureBigInteger BitwiseAnd(SecureBigInteger a, SecureBigInteger b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        var result = new uint[maxLen];
    
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var bVal = b.TryGetLimb(i, 0);
            result[i] = aVal & bVal;
        }
    
        return result;
    }
    public static SecureBigInteger BitwiseOr(SecureBigInteger a, SecureBigInteger b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        var result = new uint[maxLen];
    
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var bVal = b.TryGetLimb(i, 0);
            result[i] = aVal | bVal;
        }
    
        return result;
    }
    public static SecureBigInteger BitwiseXor(SecureBigInteger a, SecureBigInteger b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        var result = new uint[maxLen];
    
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var bVal = b.TryGetLimb(i, 0);
            result[i] = aVal ^ bVal;
        }
    
        return result;
    }
    public static SecureBigInteger BitwiseNot(SecureBigInteger a)
    {
        var result = new uint[a.Length];
        for (int i = 0; i < a.Length; i++) result[i] = ~a[i];
        return result;
    }
}
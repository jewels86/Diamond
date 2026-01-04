using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
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

    public int LogicalBitLength()
    {
        var bitLength = 0;
        var foundNonZero = 0U;
        
        for (int i = _value.Length - 1; i >= 0; i--)
        {
            var limbIsNonZero = CryptographicOperations.ConstantTime.IsNonZero(_value[i]);
            var limbBits = 32 - CountLeadingZeros(_value[i]);
            
            var thisLimbBitLength = i * 32 + limbBits;
            
            var shouldUpdate = limbIsNonZero & ~foundNonZero;
            bitLength = (int)CryptographicOperations.ConstantTime.Select(shouldUpdate, (uint)thisLimbBitLength, (uint)bitLength);
            
            foundNonZero |= limbIsNonZero;
        }
        
        return bitLength;
    }
    
    private static int CountLeadingZeros(uint value)
    {
        var count = 0;
        var stopCounting = 0U;
    
        for (int bit = 31; bit >= 0; bit--)
        {
            var bitIsZero = CryptographicOperations.ConstantTime.IsZero(value >> bit & 1);
            var shouldIncrement = bitIsZero & ~stopCounting;
            count += (int)shouldIncrement;
        
            var bitIsOne = CryptographicOperations.ConstantTime.IsNonZero(value >> bit & 1);
            stopCounting |= bitIsOne;
        }
    
        return count;
    }
}
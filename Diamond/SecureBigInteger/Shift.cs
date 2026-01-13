using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator <<(SecureBigInteger big, int shift) => LeftShift(big, shift);
    public static SecureBigInteger operator >>(SecureBigInteger big, int shift) => RightShift(big, shift);
    public static SecureBigInteger LeftShift(SecureBigInteger big, int totalShift)
    {
        if (totalShift < 0) return RightShift(big, -totalShift);
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;

        var resultLength = big.Length + limbShift + (int)CryptographicOperations.ConstantTime.IsPositive(bitShift);
        var result = new uint[resultLength];
    
        for (int i = 0; i < resultLength; i++)
        {
            var source = i - limbShift;

            var low = big.TryGetLimb(source, 0);
            var high = big.TryGetLimb(source - 1, 0);
        
            var needsHigh = (uint)(-bitShift >> 31) & 1;
            var rightShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
            var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high >> (int)rightShiftAmount, 0U);
        
            var shifted = low << bitShift | highPart;
            result[i] = shifted;
        }

        return new(result);
    }
    
    public static SecureBigInteger RightShift(SecureBigInteger big, int totalShift)
    {
        if (totalShift < 0) return LeftShift(big, -totalShift);
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;
    
        var resultLength = Math.Max(1, big.Length - limbShift);
        var result = new uint[resultLength];
    
        for (int i = 0; i < resultLength; i++)
        {
            var source = i + limbShift;

            var low = big.TryGetLimb(source, 0);
            var high = big.TryGetLimb(source + 1, 0);
        
            var needsHigh = (uint)(-bitShift >> 31) & 1;
            var leftShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
            var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high << (int)leftShiftAmount, 0U);
        
            var shifted = low >> bitShift | highPart;
            result[i] = shifted;
        }

        return new(result);
    }
    
    public static SecureBigInteger ParanoidLeftShift(SecureBigInteger big, int totalShift, int fixedIterations)
    {
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;

        var resultLength = big.Length + limbShift + (int)CryptographicOperations.ConstantTime.IsPositive(bitShift);
        var result = new SecureBigInteger(new uint[resultLength]);
    
        for (int i = 0; i < fixedIterations; i++)
        {
            var source = i - limbShift;

            var low = big.TryGetLimb(source, 0);
            var high = big.TryGetLimb(source - 1, 0);
        
            var needsHigh = (uint)(-bitShift >> 31) & 1;
            var rightShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
            var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high >> (int)rightShiftAmount, 0U);
        
            var shifted = low << bitShift | highPart;
            result.TrySetLimb(i, shifted);
        }

        return result;
    }
    
    public static SecureBigInteger ParanoidRightShift(SecureBigInteger big, int totalShift, int fixedIterations)
    {
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;

        var resultLength = Math.Max(1, big.Length - limbShift);
        var result = new SecureBigInteger(new uint[resultLength]);

        for (int i = 0; i < fixedIterations; i++)
        {
            var source = i + limbShift;

            var low = big.TryGetLimb(source, 0);
            var high = big.TryGetLimb(source + 1, 0);
    
            var needsHigh = (uint)(-bitShift >> 31) & 1;
            var leftShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
            var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high << (int)leftShiftAmount, 0U);
    
            var shifted = low >> bitShift | highPart;
            result.TrySetLimb(i, shifted);
        }

        return result;
    }
    
    public static SecureBigInteger OpSrT(SecureBigInteger value, int shiftBits, int resultLimbs)
    {
        var shiftLimbs = shiftBits / 32;
        var shiftRemainder = shiftBits % 32;
    
        var result = new uint[resultLimbs];

        if (shiftRemainder == 0)
            for (int i = 0; i < resultLimbs && shiftLimbs + i < value.Length; i++)
                result[i] = value.OpTryGetLimb(shiftLimbs + i, 0);
        else
        {
            var leftShift = 32 - shiftRemainder;
            for (int i = 0; i < resultLimbs; i++)
            {
                var low = value.OpTryGetLimb(shiftLimbs + i, 0);
                var high = value.OpTryGetLimb(shiftLimbs + i + 1, 0);
                result[i] = (low >> shiftRemainder) | (high << leftShift);
            }
        }
    
        return new(result);
    }
}
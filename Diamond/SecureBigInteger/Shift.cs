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
}
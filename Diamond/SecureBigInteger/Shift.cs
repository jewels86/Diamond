using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator <<(SecureBigInteger big, int shift) => LeftShift(big, shift);
    public static SecureBigInteger operator >>(SecureBigInteger big, int shift) => RightShift(big, shift);
    
    public static SecureBigInteger LeftShift(SecureBigInteger big, int shift) =>
        UseOptimal(() => AcceleratedLeftShift(big, shift), () => HostLeftShift(big, shift), big);
    public static SecureBigInteger RightShift(SecureBigInteger big, int shift) =>
        UseOptimal(() => AcceleratedRightShift(big, shift), () => HostRightShift(big, shift), big);
    
    #region Host
    public static SecureBigInteger HostLeftShift(SecureBigInteger big, int totalShift)
    {
        var hostBig = big.AsHost();
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;

        var resultLength = hostBig.Length + limbShift + (int)CryptographicOperations.ConstantTime.IsPositive(bitShift);
        var result = new uint[resultLength];
    
        for (int i = 0; i < resultLength; i++)
        {
            var source = i - limbShift;
        
            var low = CryptographicOperations.ConstantTime.TryGetLimb(hostBig, source, 0);
            var high = CryptographicOperations.ConstantTime.TryGetLimb(hostBig, source - 1, 0);
        
            var needsHigh = (uint)(-bitShift >> 31) & 1;
            var rightShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
            var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high >> (int)rightShiftAmount, 0U);
        
            var shifted = low << bitShift | highPart;
            result[i] = shifted;
        }

        return new(result);
    }
    
    public static SecureBigInteger HostRightShift(SecureBigInteger big, int totalShift)
    {
        var hostBig = big.AsHost();
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;
    
        var resultLength = Math.Max(1, hostBig.Length - limbShift);
        var result = new uint[resultLength];
    
        for (int i = 0; i < resultLength; i++)
        {
            var source = i + limbShift;
        
            var low = CryptographicOperations.ConstantTime.TryGetLimb(hostBig, source, 0);
            var high = CryptographicOperations.ConstantTime.TryGetLimb(hostBig, source + 1, 0);
        
            var needsHigh = (uint)(-bitShift >> 31) & 1;
            var leftShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
            var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high << (int)leftShiftAmount, 0U);
        
            var shifted = low >> bitShift | highPart;
            result[i] = shifted;
        }

        return new(result);
    }
    #endregion
    #region Accelerated
    public static SecureBigInteger AcceleratedLeftShift(SecureBigInteger big, int shift)
    {
        var acceleratedBig = big.AsAccelerated();
        var aidx = acceleratedBig.AcceleratorIndex;
        var limbShift = shift / 32;
        var resultLength = acceleratedBig.TotalSize + limbShift + 1;
        var result = Compute.Get(aidx, resultLength);
        
        Compute.Call(aidx, LeftShiftKernel, acceleratedBig.TotalSize, result, acceleratedBig, shift);
        
        return new(result);
    }

    public static SecureBigInteger AcceleratedRightShift(SecureBigInteger big, int shift)
    {
        var acceleratedBig = big.AsAccelerated();
        var aidx = acceleratedBig.AcceleratorIndex;
        var limbShift = shift / 32;
        var resultLength = Math.Max(1, acceleratedBig.TotalSize - limbShift);
        var result = Compute.Get(aidx, resultLength);
        
        Compute.Call(aidx, RightShiftKernel, acceleratedBig.TotalSize, result, acceleratedBig, shift);
        
        return new(result);
    }
    #endregion

    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, int>> LeftShiftKernel { get; } = new((i, result, input, totalShift) =>
    {
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;

        var source = (int)i - limbShift;

        var low = CryptographicOperations.ConstantTime.TryGetLimb(input, source, 0);
        var high = CryptographicOperations.ConstantTime.TryGetLimb(input, source - 1, 0);

        var needsHigh = (uint)(-bitShift >> 31) & 1;
        var rightShiftAmount = CryptographicOperations.ConstantTime.Select(needsHigh, (uint)(32 - bitShift), 0U);
        var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high >> (int)rightShiftAmount, 0U);

        var shifted = low << bitShift | highPart;
        result[i] = shifted.AsFloat();
    });
    
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, int>> RightShiftKernel { get; } = new((index, result, input, totalShift) =>
    {
        var limbShift = totalShift / 32;
        var bitShift = totalShift % 32;
    
        var sourceIndex = (int)index + limbShift;
    
        var low = CryptographicOperations.ConstantTime.TryGetLimb(input, sourceIndex, 0);
        var high = CryptographicOperations.ConstantTime.TryGetLimb(input, sourceIndex + 1, 0);
    
        var needsHigh = (uint)(-bitShift >> 31) & 1;
        var highPart = CryptographicOperations.ConstantTime.Select(needsHigh, high << 32 - bitShift, 0U);
    
        var shifted = low >> bitShift | highPart;
        result[index] = shifted.AsFloat();
    });
    #endregion
}
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger
{
    public static AcceleratedBigInteger operator <<(AcceleratedBigInteger big, int shift) => LeftShift(big, shift);
    public static AcceleratedBigInteger operator >>(AcceleratedBigInteger big, int shift) => RightShift(big, shift);
    
    public static AcceleratedBigInteger LeftShift(AcceleratedBigInteger big, int shift)
    {
        var aidx = big.AcceleratorIndex;
        var limbShift = shift / 32;
        var resultLength = big.Length + limbShift + 1;
        var result = Compute.Get(aidx, resultLength);
        
        Compute.Call(aidx, LeftShiftKernel, big.Length, result, big, shift);
        
        return new(result);
    }

    public static AcceleratedBigInteger RightShift(AcceleratedBigInteger big, int shift)
    {
        var aidx = big.AcceleratorIndex;
        var limbShift = shift / 32;
        var resultLength = Math.Max(1, big.Length - limbShift);
        var result = Compute.Get(aidx, resultLength);
        
        Compute.Call(aidx, RightShiftKernel, big.Length, result, big, shift);
        
        return new(result);
    }

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
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger
{
    // in both add and subtract, we create a buffer of size maxLen * 2 and a buffer of size maxLen
    // we call the parallelized add or subtract kernel, then reduce to fill the result
    // because of lazulite's buffer pooling, allocating buffers like sums or diffs is free
    // we will reuse them for later additions or subtractions
    // the kernels are the main bottleneck; for small maxLen, it would be faster to do this on the CPU unaccelerated
    // but because we need to keep values in the accelerated space, we'll use kernels and parallelize to make sure it runs well for large maxLen
    
    public static AcceleratedBigInteger operator +(AcceleratedBigInteger a, AcceleratedBigInteger b) => Add(a, b);
    public static AcceleratedBigInteger operator -(AcceleratedBigInteger a, AcceleratedBigInteger b) => Subtract(a, b);
    
    public static AcceleratedBigInteger Add(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var aidx = a.AcceleratorIndex;
        var maxLen = Math.Max(a.Length, b.Length);
        
        var sums = Compute.Get(aidx, maxLen * 2);
        var result = Compute.Get(aidx, maxLen);
        
        Compute.Call(aidx, AddKernel, maxLen, sums, a, b);
        Compute.Call(aidx, AddReductionKernel, 1, result, sums);
        sums.Return();
        
        return new(result);
    }

    public static AcceleratedBigInteger Subtract(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var aidx = a.AcceleratorIndex;
        var maxLen = Math.Max(a.Length, b.Length);
        
        var diffs = Compute.Get(aidx, maxLen * 2);
        var result = Compute.Get(aidx, maxLen);
        
        Compute.Call(aidx, SubtractKernel, maxLen, diffs, a, b);
        Compute.Call(aidx, SubtractReductionKernel, 1, result, diffs);
        diffs.Return();
        
        return new(result);
    }

    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>, 
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> AddKernel { get; } = new((i, sums, a, b) =>
    {
        var aVal = CryptographicOperations.ConstantTime.TryGetLimb(a, i, 0);
        var bVal = CryptographicOperations.ConstantTime.TryGetLimb(b, i, 0);
        var (high, low) = ((ulong)aVal + bVal).AsFloats();
        sums[i * 2] = high;
        sums[i * 2 + 1] = low;
    });

    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> AddReductionKernel { get; } = new((_, result, sums) =>
    {
        var carry = 0U;
        for (int i = 0; i < sums.Length / 2; i++)
        {
            var sum = (sums[i * 2], sums[i * 2 + 1]).AsULong() + carry;
            result[i] = ((uint)sum).AsFloat();
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        result[sums.Length / 2] = carry;
    });
    
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>, 
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> SubtractKernel { get; } = new((i, diffs, a, b) =>
    {
        var aVal = CryptographicOperations.ConstantTime.TryGetLimb(a, i, 0);
        var bVal = CryptographicOperations.ConstantTime.TryGetLimb(b, i, 0);
        var (high, low) = ((ulong)aVal - bVal).AsFloats();
        diffs[i * 2] = high;
        diffs[i * 2 + 1] = low;
    });
    
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> SubtractReductionKernel { get; } = new((_, result, diffs) =>
    {
        var borrow = 0U;
        for (int i = 0; i < diffs.Length / 2; i++)
        {
            var diff = (diffs[i * 2], diffs[i * 2 + 1]).AsULong() - borrow;
            result[i] = ((uint)diff).AsFloat();
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
    });
    #endregion

    // these are CPU implementations to help me understand what im doing
    
    private static uint[] TestAdd(uint[] a, uint[] b)
    {
        var carry = 0U;
        var result = new uint[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            var sum = (ulong)a[i] + b[i] + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        result[a.Length] = carry;
        return result;
    }
    
    private static uint[] TestSubtract(uint[] a, uint[] b)
    {
        var borrow = 0U;
        var result = new uint[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            var diff = (ulong)a[i] - b[i] - borrow;
            result[i] = (uint)diff;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return result;
    }
}
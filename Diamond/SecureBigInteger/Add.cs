using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator +(SecureBigInteger a, SecureBigInteger b) => Add(a, b);
    public static SecureBigInteger operator -(SecureBigInteger a, SecureBigInteger b) => Subtract(a, b);
    
    public static SecureBigInteger Add(SecureBigInteger a, SecureBigInteger b) => UseOptimal(() => AcceleratedAdd(a, b), () => HostAdd(a, b), a, b);
    public static SecureBigInteger Subtract(SecureBigInteger a, SecureBigInteger b) => UseOptimal(() => AcceleratedSubtract(a, b), () => HostSubtract(a, b), a, b);
    
    #region Host
    public static SecureBigInteger HostAdd(SecureBigInteger a, SecureBigInteger b)
    {
        var (hostA, hostB) = (a.AsHost(), b.AsHost());
        var maxLen = Math.Max(hostA.Length, hostB.Length);

        var carry = 0U;
        var result = new uint[maxLen + 1];
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = CryptographicOperations.ConstantTime.TryGetLimb(hostA, i, 0);
            var bVal = CryptographicOperations.ConstantTime.TryGetLimb(hostB, i, 0);
            var sum = (ulong)aVal + bVal + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        result[maxLen] = carry;
        
        return new(result);
    }

    public static SecureBigInteger HostSubtract(SecureBigInteger a, SecureBigInteger b)
    {
        var (hostA, hostB) = (a.AsHost(), b.AsHost());
        var maxLen = Math.Max(hostA.Length, hostB.Length);
        
        var borrow = 0U;
        var result = new uint[maxLen];
        for (int i = 0; i < maxLen; i++)
        {
            var aVal = CryptographicOperations.ConstantTime.TryGetLimb(hostA, i, 0);
            var bVal = CryptographicOperations.ConstantTime.TryGetLimb(hostB, i, 0);
            var diff = (ulong)aVal - bVal - borrow;
            result[i] = (uint)diff;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return new(result);
    }
    #endregion
    #region Accelerated
    // in both add and subtract, we create a buffer of size maxLen * 2 and a buffer of size maxLen
    // we call the parallelized add or subtract kernel, then reduce to fill the result
    // because of lazulite's buffer pooling, allocating buffers like sums or diffs is free
    // we will reuse them for later additions or subtractions
    // the kernels are the main bottleneck; for small maxLen, it would be faster to do this on the CPU unaccelerated
    // but because we need to keep values in the accelerated space, we'll use kernels and parallelize to make sure it runs well for large maxLen
    
    public static SecureBigInteger AcceleratedAdd(SecureBigInteger a, SecureBigInteger b)
    {
        var (acceleratedA, acceleratedB) = (a.AsAccelerated(), b.AsAccelerated());
        var aidx = acceleratedA.AcceleratorIndex;
        var maxLen = Math.Max(acceleratedA.TotalSize, acceleratedB.TotalSize);
        
        var sums = Compute.Get(aidx, maxLen * 2);
        var result = Compute.Get(aidx, maxLen + 1);
        
        Compute.Call(aidx, AddKernel, maxLen, sums, acceleratedA, acceleratedB);
        Compute.Call(aidx, AddReductionKernel, 1, result, sums);
        sums.Return();
        
        return new(result);
    }

    public static SecureBigInteger AcceleratedSubtract(SecureBigInteger a, SecureBigInteger b)
    {
        var (acceleratedA, acceleratedB) = (a.AsAccelerated(), b.AsAccelerated());
        var aidx = acceleratedA.AcceleratorIndex;
        var maxLen = Math.Max(acceleratedA.TotalSize, acceleratedB.TotalSize);
        
        var diffs = Compute.Get(aidx, maxLen * 2);
        var result = Compute.Get(aidx, maxLen);
        
        Compute.Call(aidx, SubtractKernel, maxLen, diffs, acceleratedA, acceleratedB);
        Compute.Call(aidx, SubtractReductionKernel, 1, result, diffs);
        diffs.Return();
        
        return new(result);
    }
    #endregion
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
}
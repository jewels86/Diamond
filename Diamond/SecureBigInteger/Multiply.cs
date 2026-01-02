using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator *(SecureBigInteger a, SecureBigInteger b) => Multiply(a, b);
    
    public static SecureBigInteger Multiply(SecureBigInteger a, SecureBigInteger b) =>
        UseOptimal(() => AcceleratedMultiply(a, b), () => HostMultiply(a, b), a, b);

    #region Host
    public static SecureBigInteger HostMultiply(SecureBigInteger a, SecureBigInteger b)
    {
        var (hostA, hostB) = (a.AsHost(), b.AsHost());
        var resultSize = hostA.Length + hostB.Length;
        var result = new uint[resultSize];

        for (int i = 0; i < hostA.Length; i++)
        {
            var aVal = CryptographicOperations.ConstantTime.TryGetLimb(hostA, i, 0);
            var carry = 0UL;
            for (int j = 0; j < hostB.Length; j++)
            {
                var bVal = CryptographicOperations.ConstantTime.TryGetLimb(hostB, j, 0);
                var product = (ulong)aVal * bVal + result[i + j] + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            result[i + hostB.Length] = (uint)carry;
        }
        
        return new(result);
    }
    #endregion
    #region Accelerated
    // this one allocates 2 buffers of resultSize * 2 and totalSize * 2
    // for reference, with 2 128-limb numbers that 256 * 2 and 16,384 * 2
    // that's why we do two reductions instead of one - that sequential kernel would have had to iterate through all products otherwise
    // this way, it only has to iterate through resultSize times, and the rest is parallel
    // if we hadn't parallelized, we would have had to iterate through totalSize times sequentially
    
    public static SecureBigInteger AcceleratedMultiply(SecureBigInteger a, SecureBigInteger b)
    {
        var (acceleratedA, acceleratedB) = (a.AsAccelerated(), b.AsAccelerated());
        var aidx = acceleratedA.AcceleratorIndex;
        var totalProducts = acceleratedA.TotalSize * acceleratedB.TotalSize;
        var resultSize = acceleratedA.TotalSize + acceleratedB.TotalSize;
        
        var products = Compute.Get(aidx, totalProducts * 2);
        var reduced = Compute.Get(aidx, resultSize * 2);
        var result = Compute.Get(aidx, resultSize);
        
        Compute.Call(aidx, MultiplyKernel, totalProducts, products, acceleratedA, acceleratedB);
        Compute.Call(aidx, MultiplyReductionKernel, resultSize, reduced, products, acceleratedA.TotalSize, acceleratedB.TotalSize);
        Compute.Call(aidx, MultiplyPropagationKernel, 1, result, reduced);
        Compute.Return(products, reduced);
        
        return new(result);
    }
    #endregion
    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> MultiplyKernel { get; } = new((index, products, a, b) =>
    {
        var (i, j) = KernelProgramming.MatrixFromIndex(index, b.IntLength);
        var aVal = CryptographicOperations.ConstantTime.TryGetLimb(a, i, 0);
        var bVal = CryptographicOperations.ConstantTime.TryGetLimb(b, j, 0);
        var (high, low) = ((ulong)aVal * bVal).AsFloats();
        products[index * 2] = high;
        products[index * 2 + 1] = low;
    });

    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, int, int>> MultiplyReductionKernel { get; } = new((index, reduced, products, aLength, bLength) =>
    {
        var k = (int)index;
        var start = XMath.Max(0, k - (bLength - 1));
        var end = XMath.Min(k, aLength - 1);

        var sum = 0UL;
        for (int i = start; i <= end; i++)
        {
            var j = k - i;
            var target = i * bLength + j;
            sum += (products[target * 2], products[target * 2 + 1]).AsULong();
        }
        var (high, low) = sum.AsFloats();
        reduced[k * 2] = high;
        reduced[k * 2 + 1] = low;
    });

    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> MultiplyPropagationKernel { get; } = new((index, result, reduced) =>
    {
        var carry = 0UL;
        for (int k = 0; k < reduced.Length / 2; k++)
        {
            var sum = (reduced[k * 2], reduced[k * 2 + 1]).AsULong() + carry;
            result[k] = ((uint)sum).AsFloat();
            carry = sum >> 32;
        }
        result[reduced.Length / 2] = ((uint)carry).AsFloat();
    });
    #endregion
}
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger
{
    // this one allocates 2 buffers of resultSize * 2 and totalSize * 2
    // for reference, with 2 128-limb numbers that 256 * 2 and 16,384 * 2
    // that's why we do two reductions instead of one - that sequential kernel would have had to iterate through all products otherwise
    // this way, it only has to iterate through resultSize times
    // if we hadn't parallelized, we would have had to iterate through totalSize times sequentially
    
    public static AcceleratedBigInteger operator *(AcceleratedBigInteger a, AcceleratedBigInteger b) => Multiply(a, b);
    
    public static AcceleratedBigInteger Multiply(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var aidx = a.AcceleratorIndex;
        var totalProducts = a.Length * b.Length;
        var resultSize = a.Length + b.Length;
        
        var products = Compute.Get(aidx, totalProducts * 2);
        var reduced = Compute.Get(aidx, resultSize * 2);
        var result = Compute.Get(aidx, resultSize);
        
        Compute.Call(aidx, MultiplyKernel, totalProducts, products, a, b);
        Compute.Call(aidx, MultiplyReductionKernel, resultSize, reduced, products, a.Length, b.Length);
        Compute.Call(aidx, MultiplyPropagationKernel, 1, result, reduced);
        Compute.Return(products, reduced);
        
        return new(result);
    }
    
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

    private static uint[] TestMultiply(uint[] a, uint[] b)
    {
        var result = new uint[a.Length + b.Length];
        for (int i = 0; i < a.Length; i++)
        {
            var carry = 0UL;
            for (int j = 0; j < b.Length; j++)
            {
                var product = (ulong)a[i] * b[j] + result[i + j] + carry;
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            result[i + b.Length] = (uint)carry;
        }
        return result;
    }
}
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    private static SecureBigInteger Multiply(SecureBigInteger a, SecureBigInteger b)
    {
        // this is the naive implementation, O(n^2)
        var result = new uint[a.Length + b.Length];

        for (int i = 0; i < a.Length; i++)
        {
            var carry = 0UL;
            for (int j = 0; j < b.Length; j++)
            {
                var product = (ulong)a[i] * b[j] + result[i + j] + carry; // multiply and add to existing result + carry
                result[i + j] = (uint)product;
                carry = product >> 32;
            }
            result[i + b.Length] = (uint)carry;
        }
        
        return new SecureBigInteger(result);
    }

    private static SecureBigInteger AcceleratedMultiply(SecureBigInteger a, SecureBigInteger b)
    {
        var resultLength = a.Length + b.Length;
        
        var totalProducts = a.Length * b.Length;
        var products = Compute.Get(a.AcceleratorIndex, totalProducts * 2);
        Compute.Call(a.AcceleratorIndex, MultiplyKernels, totalProducts, products, a.GetAccelerated(), b.GetAccelerated());
        
        var reduced = Compute.Get(a.AcceleratorIndex, resultLength * 2);
        Compute.Call(a.AcceleratorIndex, MultiplyReductionKernels, resultLength, reduced, products);
        
        Compute.Synchronize(a.AcceleratorIndex);
        var resultFloats = reduced.GetAsArray1D();
        var result = new uint[resultLength];

        var carry = 0UL;
        for (int i = 0; i < resultLength; i++)
        {
            var sum = (resultFloats[i * 2], resultFloats[i * 2 + 1]).AsULong();
            sum += carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        
        Compute.Return(products, reduced);
        
        return new SecureBigInteger(result);
    }

    private static Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>[] MultiplyKernels { get; } = Compute.Load((index, r, a, b) =>
    {
        var (i, j) = KernelProgramming.MatrixFromIndex(index, b.IntLength);
        var k = i + j;
        var baseIndex = CryptographicOperations.TriangularNumber(k) * 2;

        var product = a[i].AsULong() * b[j].AsUInt();
        var (low, high) = product.AsFloats();
        r[baseIndex + i * 2] = low;
        r[baseIndex + i * 2 + 1] = high;
    });

    private static Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>[] MultiplyReductionKernels { get; } = Compute.Load((k, r, products) =>
    {
        var baseIndex = CryptographicOperations.TriangularNumber(k) * 2;
        var length = k + 1;

        var sum = 0UL;
        for (int i = 0; i < length; i++)
            sum += (products[baseIndex + i * 2], products[baseIndex + i * 2 + 1]).AsULong();

        var (low, high) = sum.AsFloats();
        r[k * 2] = low;
        r[k * 2 + 1] = high;
    });
}
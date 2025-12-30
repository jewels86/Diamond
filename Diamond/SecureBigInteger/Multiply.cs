using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator *(SecureBigInteger a, SecureBigInteger b) => AcceleratedMultiply(a, b);
    
    #region Multiplication
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
        Compute.Call(a.AcceleratorIndex, MultiplyKernel, totalProducts, products, a.GetAccelerated(), b.GetAccelerated());
        
        var reduced = Compute.Get(a.AcceleratorIndex, resultLength * 2);
        var carries = Compute.Get(a.AcceleratorIndex, resultLength * 2);
        Compute.Call(a.AcceleratorIndex, MultiplyReductionKernel, resultLength, reduced, products, carries, a.Length, b.Length);
        
        Compute.Synchronize(a.AcceleratorIndex);
        var carryFloats = carries.GetAsArray1D();
        var resultFloats = reduced.GetAsArray1D();
        var result = new uint[resultLength];

        var carry = 0UL;
        for (int i = 0; i < resultLength; i++)
        {
            var gpuSum = (resultFloats[i * 2], resultFloats[i * 2 + 1]).AsULong();
            var gpuCarry = (carryFloats[i * 2], carryFloats[i * 2 + 1]).AsULong();
    
            var total = gpuSum + carry;
            result[i] = (uint)total;
            carry = (total >> 32) + (gpuCarry << 32);
        }
        
        Compute.Return(products, reduced);
        
        return new SecureBigInteger(result);
    }
    #endregion

    #region Kernels
    private static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> MultiplyKernel { get; } = new((index, r, a, b) =>
    {
        var (i, j) = KernelProgramming.MatrixFromIndex(index, b.IntLength);
        var k = i + j;
        var baseIndex = CryptographicOperations.TriangularNumber(k) * 2;

        var product = a[i].AsULong() * b[j].AsUInt();
        var (low, high) = product.AsFloats();
        var minI = Math.Max(0, k - (b.IntLength - 1));
        var offset = i - minI;
        r[baseIndex + offset * 2] = low;
        r[baseIndex + offset * 2 + 1] = high;
    });

    private static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, int, int>> MultiplyReductionKernel { get; } = new((k, r, products, carries, aLen, bLen) =>
    {
        var baseIndex = CryptographicOperations.TriangularNumber(k) * 2;
        
        var minI = Math.Max(0, k - (bLen - 1));
        var maxI = Math.Min(k, aLen - 1);
        var length = maxI - minI + 1;

        var sumLow = 0UL;
        var sumHigh = 0UL;
        for (int i = 0; i < length; i++)
        {
            var product = (products[baseIndex + i * 2], products[baseIndex + i * 2 + 1]).AsULong();
            var newSumLow = sumLow + product;
            var overflow = CryptographicOperations.ConstantTime.DetectAdditionOverflow(sumLow, product, newSumLow);
            sumLow = newSumLow;
            sumHigh += overflow;
        }

        var (low, high) = sumLow.AsFloats();
        r[k * 2] = low;
        r[k * 2 + 1] = high;

        var (carryLow, carryHigh) = sumHigh.AsFloats();
        carries[k * 2] = carryLow;
        carries[k * 2 + 1] = carryHigh;
    });
    #endregion
}
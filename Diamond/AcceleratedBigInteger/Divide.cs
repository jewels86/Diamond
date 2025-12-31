using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger
{
    public static AcceleratedBigInteger operator /(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var (quotient, remainder) = Divide(a, b);
        remainder.Dispose();
        return quotient;
    }

    public static AcceleratedBigInteger operator %(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var (quotient, remainder) = Divide(a, b);
        quotient.Dispose();
        return remainder;
    }
    
    public static (AcceleratedBigInteger quotient, AcceleratedBigInteger remainder) Divide(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var aidx = a.AcceleratorIndex;
        var quotientLength = Math.Max(1, a.Length - b.Length + 1);
        var remainderLength = a.Length;
    
        var quotient = Compute.Get(aidx, quotientLength);
        var remainder = Compute.Get(aidx, remainderLength);
    
        Compute.Call(aidx, DivideKernel, 1, quotient, remainder, a, b);
    
        return (new(quotient), new(remainder));
    }

    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> DivideKernel { get; } = new((_, quotient, remainder, a, b) =>
    {
        for (int i = 0; i < a.IntLength; i++) remainder[i] = a[i];
        
        for (int i = a.IntLength - 1; i >= b.IntLength - 1; i--)
        {
            var topRemainder = (ulong)remainder[i] << 32 | remainder[i - 1].AsUInt();
            var topDivisor = b[b.IntLength - 1].AsUInt();
            var qEstimate = (uint)(topRemainder / topDivisor);

            var borrow = 0UL;
            for (int j = 0; j < b.IntLength; j++)
            {
                var product = (ulong)qEstimate * b[j].AsUInt();
                var diff = (ulong)remainder[i - b.IntLength + 1 + j].AsUInt() - (uint)product - borrow;
                remainder[i - b.IntLength + 1 + j] = ((uint)diff).AsFloat();
                borrow = (product >> 32) + (diff >> 32 & 1);
            }

            var overestimated = (uint)(-(long)borrow >> 63) & 1;
            var correctedQ = qEstimate - overestimated;
            quotient[i - b.IntLength + 1] = correctedQ.AsFloat();

            var carry = 0UL;
            for (int j = 0; j < b.IntLength; j++)
            {
                var bValue = CryptographicOperations.ConstantTime.Select(overestimated, b[j].AsUInt(), 0U);
                var sum = (ulong)remainder[i - b.IntLength + 1 + j].AsUInt() + bValue + carry;
                remainder[i - b.IntLength + 1 + j] = ((uint)sum).AsFloat();
                carry = sum >> 32;
            }
        }
    });
}
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger
{
    public static AcceleratedBigInteger ConditionalSelect(ScalarValue condition, AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var aidx = a.AcceleratorIndex;
        var maxLen = Math.Max(a.Length, b.Length);
        var result = Compute.Get(aidx, maxLen);
    
        Compute.Call(aidx, ConditionalSelectKernel, maxLen, result, condition, a, b);
    
        return new(result);
    }
    
    public static AcceleratedBigInteger ConditionalSubtract(AcceleratedBigInteger a, AcceleratedBigInteger b) => ConditionalSelect(a >= b, a - b, a);

    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> ConditionalSelectKernel { get; } = new((index, result, condition, a, b) =>
    {
        var cond = condition[0].AsUInt();
        var aVal = CryptographicOperations.ConstantTime.TryGetLimb(a, index, 0);
        var bVal = CryptographicOperations.ConstantTime.TryGetLimb(b, index, 0);
        var selected = CryptographicOperations.ConstantTime.Select(cond, aVal, bVal);
        
        result[index] = selected.AsFloat();
    });
}
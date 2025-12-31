using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger
{
    public static ScalarValue operator >=(AcceleratedBigInteger a, AcceleratedBigInteger b) => GreaterThanOrEqual(a, b);
    public static ScalarValue operator <=(AcceleratedBigInteger a, AcceleratedBigInteger b) => GreaterThanOrEqual(b, a);
    
    public static ScalarValue GreaterThanOrEqual(AcceleratedBigInteger a, AcceleratedBigInteger b)
    {
        var aidx = a.AcceleratorIndex;
        var maxLen = Math.Max(a.Length, b.Length);

        var diffs = Compute.Get(aidx, maxLen * 2);
        var borrowOut = Compute.Get(aidx, 1);
        var one = new ScalarValue(1, aidx);
        
        Compute.Call(aidx, SubtractKernel, maxLen, diffs, a, b);
        Compute.Call(aidx, ModifiedSubtractReductionKernel, 1, borrowOut, diffs);
        diffs.Return();

        return one - new ScalarValue(borrowOut);
    }
    
    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> ModifiedSubtractReductionKernel { get; } = new((_, borrowOut, diffs) =>
    {
        var borrow = 0U;
        for (int i = 0; i < diffs.Length / 2; i++)
        {
            var diff = (diffs[i * 2], diffs[i * 2 + 1]).AsULong() - borrow;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        borrowOut[0] = borrow.AsFloat();
    });
    #endregion
}
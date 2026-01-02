using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static ConditionalValue operator >=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(a, b);
    public static ConditionalValue operator <=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(b, a);
    
    public static ConditionalValue GreaterThanOrEqual(SecureBigInteger a, SecureBigInteger b) => 
        UseOptimal(() => AcceleratedGreaterThanOrEqual(a, b), () => HostGreaterThanOrEqual(a, b), a, b);
    
    #region Host
    public static ConditionalValue HostGreaterThanOrEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var (hostA, hostB) = (a.AsHost(), b.AsHost());
        
        var borrow = 0U;
        for (int i = 0; i < a.Length; i++)
        {
            var aVal = CryptographicOperations.ConstantTime.TryGetLimb(hostA, i, 0);
            var bVal = CryptographicOperations.ConstantTime.TryGetLimb(hostB, i, 0);
            var diff = (ulong)aVal - bVal - borrow;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return 1U - borrow;
    }
    #endregion
    #region Accelerated
    public static ConditionalValue AcceleratedGreaterThanOrEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var (acceleratedA, acceleratedB) = (a.AsAccelerated(), b.AsAccelerated());
        var aidx = acceleratedA.AcceleratorIndex;
        var maxLen = Math.Max(acceleratedA.TotalSize, acceleratedB.TotalSize);

        var diffs = Compute.Get(aidx, maxLen * 2);
        var borrowOut = Compute.Get(aidx, 1);
        var one = new ScalarValue(1, aidx);
        
        Compute.Call(aidx, SubtractKernel, maxLen, diffs, acceleratedA, acceleratedB);
        Compute.Call(aidx, ModifiedSubtractReductionKernel, 1, borrowOut, diffs);
        diffs.Return();

        return one - new ScalarValue(borrowOut);
    }
    #endregion
    
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

public class ConditionalValue
{
    private ScalarValue? _accelerated;
    private uint? _host;
    
    public ConditionalValue(ScalarValue value) => _accelerated = value;
    public ConditionalValue(uint value) => _host = value;

    public ScalarValue AsAccelerated()
    {
        if (_accelerated is null) ToAccelerated();
        return _accelerated!;
    }

    public uint AsHost()
    {
        if (_host is null) ToHost();
        return _host!.Value;
    }

    private void ToAccelerated() => _accelerated = new(_host!.Value.AsFloat(), Compute.RequestOptimalAccelerator());
    private void ToHost() => _host = _accelerated!.ToHost().AsUInt();
    
    public static implicit operator ConditionalValue(ScalarValue value) => new(value);
    public static implicit operator ConditionalValue(uint value) => new(value);
}
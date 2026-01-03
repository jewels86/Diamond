using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static ConditionalValue operator >=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(a, b);
    public static ConditionalValue operator <=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(b, a);
    public static ConditionalValue operator ==(SecureBigInteger a, SecureBigInteger b) => Equal(a, b);
    public static ConditionalValue operator !=(SecureBigInteger a, SecureBigInteger b) => NotEqual(a, b);
    public static ConditionalValue operator >(SecureBigInteger a, SecureBigInteger b) => GreaterThan(a, b);
    public static ConditionalValue operator <(SecureBigInteger a, SecureBigInteger b) => LessThan(a, b);
    
    public static ConditionalValue GreaterThanOrEqual(SecureBigInteger a, SecureBigInteger b) => 
        UseOptimal(() => AcceleratedGreaterThanOrEqual(a, b), () => HostGreaterThanOrEqual(a, b), a, b);
    public static ConditionalValue Equal(SecureBigInteger a, SecureBigInteger b) =>
        UseOptimal(() => AcceleratedEqual(a, b), () => HostEqual(a, b), a, b);
    public static ConditionalValue NotEqual(SecureBigInteger a, SecureBigInteger b) => 
        UseOptimal(() => AcceleratedNotEqual(a, b), () => HostNotEqual(a, b), a, b);

    public static ConditionalValue GreaterThan(SecureBigInteger a, SecureBigInteger b) =>
        CryptographicOperations.ConstantTime.Not((a <= b).AsHost());
    public static ConditionalValue LessThan(SecureBigInteger a, SecureBigInteger b) =>
        CryptographicOperations.ConstantTime.Not((a >= b).AsHost());

    public static ConditionalValue IsEven(SecureBigInteger big) => CryptographicOperations.ConstantTime.Select(big.AsHost()[0] & 1, 1U, 0U);
    public static ConditionalValue IsOdd(SecureBigInteger big) => CryptographicOperations.ConstantTime.Not(IsEven(big).AsHost());

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

    public static ConditionalValue HostEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var (hostA, hostB) = (a.AsHost(), b.AsHost());

        var equal = 1U;
        for (int i = 0; i < a.Length; i++)
        {
            var aVal = CryptographicOperations.ConstantTime.TryGetLimb(hostA, i, 0);
            var bVal = CryptographicOperations.ConstantTime.TryGetLimb(hostB, i, 0);
            var isNonZero = CryptographicOperations.ConstantTime.IsNonZero(aVal - bVal);
            equal = CryptographicOperations.ConstantTime.Select(isNonZero, 0U, equal);
        }
        
        return 1U;
    }

    public static ConditionalValue HostNotEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var equal = HostEqual(a, b).AsHost();
        return CryptographicOperations.ConstantTime.Select(equal, 0U, 1U);
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

    public static ConditionalValue AcceleratedEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var (acceleratedA, acceleratedB) = (a.AsAccelerated(), b.AsAccelerated());
        var aidx = acceleratedA.AcceleratorIndex;
        var maxLen = Math.Max(acceleratedA.TotalSize, acceleratedB.TotalSize);
        var equal = new ScalarValue(1, aidx);
        
        Compute.Call(aidx, EqualKernel, maxLen, equal, acceleratedA, acceleratedB);
        return equal;
    }

    public static ConditionalValue AcceleratedNotEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var equal = AcceleratedEqual(a, b);
        var aidx = a.AcceleratorIndex;
        var result = new ScalarValue(1, aidx);
        
        Compute.Call(NotKernel, result, equal.AsAccelerated());
        return result;
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

    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> EqualKernel { get; } = new((i, r, a, b) =>
    {
        var aVal = CryptographicOperations.ConstantTime.TryGetLimb(a, i, 0);
        var bVal = CryptographicOperations.ConstantTime.TryGetLimb(b, i, 0);
        var isNonZero = CryptographicOperations.ConstantTime.IsNonZero(aVal - bVal);
        var valueToWrite = CryptographicOperations.ConstantTime.Select(isNonZero, 0U, 1U);
        Atomic.Min(ref r[0], valueToWrite.AsFloat());
    });

    public static KernelStorage<Action<Index1D, ArrayView1D<float, Stride1D.Dense>, ArrayView1D<float, Stride1D.Dense>>> NotKernel { get; } = new((i, r, a) =>
    {
        var notA = CryptographicOperations.ConstantTime.Select(a[i].AsUInt(), 0U, 1U);
        r[i] = notA.AsFloat();
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
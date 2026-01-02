using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger Select(ConditionalValue condition, SecureBigInteger a, SecureBigInteger b) => 
        UseOptimal(() => AcceleratedSelect(condition, a, b), () => HostSelect(condition, a, b), a, b);
    
    #region Host
    public static SecureBigInteger HostSelect(ConditionalValue condition, SecureBigInteger a, SecureBigInteger b)
    {
        var (hostCondition, hostA, hostB) = (condition.AsHost(), a.AsHost(), b.AsHost());
        var maxLen = Math.Max(hostA.Length, hostB.Length);
        var result = new uint[maxLen];

        for (int i = 0; i < maxLen; i++)
        {
            var aVal = CryptographicOperations.ConstantTime.TryGetLimb(hostA, i, 0);
            var bVal = CryptographicOperations.ConstantTime.TryGetLimb(hostB, i, 0);
            var selected = CryptographicOperations.ConstantTime.Select(hostCondition, aVal, bVal);
            result[i] = selected;
        }
        
        return new(result);
    }
    #endregion
    #region Accelerated
    public static SecureBigInteger AcceleratedSelect(ConditionalValue condition, SecureBigInteger a, SecureBigInteger b)
    {
        var (acceleratedCondition, acceleratedA, acceleratedB) = (condition.AsAccelerated(), a.AsAccelerated(), b.AsAccelerated());
        var aidx = acceleratedA.AcceleratorIndex;
        var maxLen = Math.Max(acceleratedA.TotalSize, acceleratedB.TotalSize);;
        var result = Compute.Get(aidx, maxLen);
    
        Compute.Call(aidx, ConditionalSelectKernel, maxLen, result, acceleratedCondition, acceleratedA, acceleratedB);
    
        return new(result);
    }
    #endregion

    #region Kernels
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
    #endregion
}
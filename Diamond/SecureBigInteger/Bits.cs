using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static uint[] GetBits(SecureBigInteger big) => UseOptimal(() => AcceleratedGetBits(big), () => HostGetBits(big), big);
    
    #region Host
    public static uint[] HostGetBits(SecureBigInteger big)
    {
        var hostBig = big.AsHost();
        var result = new uint[big.BitLength];

        for (int i = 0; i < big.BitLength; i++)
        {
            var limbIndex = i / 32;
            var bitPosition = i % 32;
            var limb = hostBig[limbIndex];
        
            result[i] = limb >> bitPosition & 1u;
        }
        
        return result;
    }
    #endregion
    
    #region Accelerated
    public static uint[] AcceleratedGetBits(SecureBigInteger big)
    {
        var acceleratedBig = big.AsAccelerated();
        var aidx = acceleratedBig.AcceleratorIndex;
        var result = new VectorValue(Compute.Get(aidx, acceleratedBig.TotalSize));
        Compute.Call(aidx, GetBitsKernel, big.BitLength, result, acceleratedBig);

        return result.ToHost().AsUInts();
    } 
    #endregion
    
    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>>> GetBitsKernel { get; } = new((i, r, a) =>
    {
        var limbIndex = i / 32;
        var bitPosition = i % 32;
        var limb = a[limbIndex].AsUInt();
    
        r[i] = limb >> bitPosition & 1u;
    });
    #endregion
}
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger Copy(SecureBigInteger source, int sourceStart, int destStart, int length) => 
        UseOptimal(() => AcceleratedCopy(source, sourceStart, destStart, length), () => HostCopy(source, sourceStart, destStart, length), source);
    
    #region Host
    public static SecureBigInteger HostCopy(SecureBigInteger source, int sourceStart, int destStart, int length)
    {
        var hostSource = source.AsHost();
        var result = new uint[length];
        
        CryptographicOperations.ConstantTime.Copy(hostSource, sourceStart, result, destStart, length);
        return new(result);
    }
    #endregion
    #region Accelerated
    public static SecureBigInteger AcceleratedCopy(SecureBigInteger source, int sourceStart, int destStart, int length)
    {
        var acceleratedSource = (source.AsAccelerated());
        var aidx = acceleratedSource.AcceleratorIndex;
        var result = Compute.Get(aidx, length);
        
        Compute.Call(aidx, CopyKernel, length, result, acceleratedSource, sourceStart, destStart);
        
        return new(result);
    }
    #endregion
    
    #region Kernels
    public static KernelStorage<Action<Index1D,
        ArrayView1D<float, Stride1D.Dense>,
        ArrayView1D<float, Stride1D.Dense>, int, int>> CopyKernel { get; } = new((i, dest, source, sourceStart, destStart) => dest[destStart + i] = source[sourceStart + i]);
    #endregion
}
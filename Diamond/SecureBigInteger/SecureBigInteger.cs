using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger : IDisposable
{
    private VectorValue? _accelerated;
    private uint[]? _host;

    #region Constructors
    public SecureBigInteger(VectorValue value)
    {
        _accelerated = value;
        Length = value.TotalSize;
    }
    public SecureBigInteger(MemoryBuffer1D<float, Stride1D.Dense> value) : this(new VectorValue(value)) { }

    public SecureBigInteger(uint[] value)
    {
        _host = value;
        Length = value.Length;
    }
    public SecureBigInteger(float[] value) : this(value.AsUInts()) { }
    public SecureBigInteger(uint value) : this([value]) { }
    #endregion

    #region Properties
    public int Length { get; }
    public int ByteLength => Length * 4;
    public int BitLength => ByteLength * 8;
    public int AcceleratorIndex => _accelerated?.AcceleratorIndex ?? -1;
    #endregion
    
    #region Methods
    private uint[] AsHost()
    {
        if (_host is null) ToHost();
        return _host!;
    }
    private VectorValue AsAccelerated()
    {
        if (_accelerated is null) ToAccelerated();
        return _accelerated!;
    }

    private void ToHost() => _host = _accelerated!.ToHost().AsUInts();
    private void ToAccelerated() => _accelerated = new(_host!.AsFloats(), Compute.RequestOptimalAccelerator());
    
    private static T UseOptimal<T>(Func<T> accelerated, Func<T> host, params SecureBigInteger[] bigs) => 
        bigs.Any(b => b.Length > OptimalLimbThreshold) ? accelerated() : host();
    
    public void Dispose() => _accelerated?.Dispose();
    #endregion

    
    #region Static Members & Constants
    public const int OptimalLimbThreshold = 32;
    
    public static SecureBigInteger Zero { get; } = new(0);
    public static SecureBigInteger One { get; } = new(1);
    public static SecureBigInteger Two { get; } = new(2);
    #endregion
}
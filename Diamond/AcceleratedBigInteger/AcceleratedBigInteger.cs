using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class AcceleratedBigInteger(VectorValue value) : IDisposable
{
    private readonly VectorValue _value = value;

    #region Constructors
    public AcceleratedBigInteger(MemoryBuffer1D<float, Stride1D.Dense> value) : this(new VectorValue(value)) { }
    public AcceleratedBigInteger(uint[] value) : this(new VectorValue(value.AsFloats(), Compute.RequestOptimalAccelerator())) { }
    public AcceleratedBigInteger(uint value) : this([value]) { }
    #endregion

    #region Properties
    public int Length => _value.TotalSize;
    public int ByteLength => Length * 4;
    public int BitLength => ByteLength * 8;
    public int AcceleratorIndex => _value.AcceleratorIndex;
    #endregion
    
    #region Methods
    public uint[] ToHost() => _value.ToHost().AsUInts();
    public byte[] ToBytes() => CryptographicOperations.FromFloats(_value.ToHost());
    
    public void Dispose() => _value.Dispose();
    #endregion

    public static implicit operator ArrayView1D<float, Stride1D.Dense>(AcceleratedBigInteger big) => big._value;
    
    #region Static Members
    public static AcceleratedBigInteger Zero { get; } = new(0);
    public static AcceleratedBigInteger One { get; } = new(1);
    public static AcceleratedBigInteger Two { get; } = new(2);
    #endregion
}
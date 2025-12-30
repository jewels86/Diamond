using Jewels.Lazulite;

namespace Diamond;

public sealed partial class SecureBigInteger(uint[] value) : IDisposable
{
    private readonly uint[] _value = CryptographicOperations.Copy(value);
    private VectorValue? _acceleratedValue;
    private bool _accelerated;

    public byte[] AsBytes()
    {
        // covert to bytes; not sure if this needs to be constant-time as well
        throw new NotImplementedException();
    }

    public static SecureBigInteger FromBytes(byte[] bytes)
    {
        // same thing here, maybe constant-time?
        throw new NotImplementedException();
    }

    public int Length => _value.Length;
    public int ByteLength => Length * 4;
    public int BitLength => ByteLength * 8;
    
    private int AcceleratorIndex => GetAccelerated().AcceleratorIndex;

    private static bool LengthsMatch(SecureBigInteger a, SecureBigInteger b, out int length)
    {
        length = a.ByteLength;
        return length == b.ByteLength;
    }

    public void Accelerate()
    {
        if (_acceleratedValue is not null) return;
        _acceleratedValue = new(_value.AsFloats(), Compute.RequestOptimalAccelerator());
    }

    private VectorValue GetAccelerated()
    {
        if (_acceleratedValue is null) Accelerate();
        return _acceleratedValue!;
    }

    private uint this[int index] => _value[index];

    public void Dispose() => _acceleratedValue?.Dispose();
}
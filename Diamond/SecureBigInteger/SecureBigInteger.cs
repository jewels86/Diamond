using System.Numerics;
using System.Text;
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    private readonly uint[] _value;

    #region Constructors
    public SecureBigInteger(uint[] value) => _value = value;
    public SecureBigInteger(float[] value) : this(value.AsUInts()) { }
    public SecureBigInteger(uint value) : this([value]) { }
    public SecureBigInteger(ulong value)
    {
        _value = new uint[2];
        var asFloats = value.AsFloats();
        _value[0] = asFloats.low.AsUInt();
        _value[1] = asFloats.high.AsUInt();
    }

    public SecureBigInteger(BigInteger big)
    {
        var biggie = FromBytes(big.ToByteArray());
        _value = biggie._value;
    }
    #endregion

    #region Properties
    public int Length => _value.Length;
    public int ByteLength => Length * 4;
    public int BitLength => ByteLength * 8;
    #endregion
    
    #region Methods
    public override string ToString()
    {
        int firstNonZero = _value.Length - 1;
        while (firstNonZero > 0 && _value[firstNonZero] == 0) firstNonZero--;
    
        var sb = new StringBuilder("0x");
        sb.Append(_value[firstNonZero].ToString("x"));
        for (int i = firstNonZero - 1; i >= 0; i--) 
            sb.Append(_value[i].ToString("x8"));
    
        return sb.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SecureBigInteger other) return false;
        return other == this == 1U;
    }

    public override int GetHashCode() => _value.GetHashCode();

    private uint TryGetLimb(int i, uint elseValue) => CryptographicOperations.ConstantTime.TryGetLimb(_value, i, elseValue);
    private uint this[int index]
    {
        get => _value[index];
        set => _value[index] = value;
    }
    
    public static implicit operator SecureBigInteger(uint[] value) => new(value);
    public static implicit operator SecureBigInteger(uint value) => new(value);

    public byte[] ToBytes()
    {
        var lastNonZeroIndex = _value.Length - 1;
        while (lastNonZeroIndex > 0 && _value[lastNonZeroIndex] == 0)
            lastNonZeroIndex--;
    
        var byteCount = (lastNonZeroIndex + 1) * 4;
        var bytes = new byte[byteCount];
    
        for (int i = 0; i <= lastNonZeroIndex; i++)
        {
            var limb = _value[i];
            bytes[i * 4] = (byte)limb;
            bytes[i * 4 + 1] = (byte)(limb >> 8);
            bytes[i * 4 + 2] = (byte)(limb >> 16);
            bytes[i * 4 + 3] = (byte)(limb >> 24);
        }
    
        return bytes;
    }

    public static SecureBigInteger FromBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
            return Zero;
    
        var limbCount = (bytes.Length + 3) / 4;
        var limbs = new uint[limbCount];
    
        for (int i = 0; i < bytes.Length; i++)
        {
            var limbIndex = i / 4;
            var byteInLimb = i % 4;
            limbs[limbIndex] |= (uint)bytes[i] << (byteInLimb * 8);
        }
    
        return new SecureBigInteger(limbs);
    }
    #endregion
    
    #region Static Members & Constants
    public static SecureBigInteger Zero { get; } = new(0);
    public static SecureBigInteger One { get; } = new(1);
    public static SecureBigInteger Two { get; } = new(2);
    #endregion
}
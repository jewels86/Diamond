using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger Copy(SecureBigInteger source, int sourceStart, int destStart, int length)
    {
        var result = new uint[length];
        CryptographicOperations.ConstantTime.Copy(source._value, sourceStart, result, destStart, length);
        return new(result);
    }

    public static SecureBigInteger Pad(SecureBigInteger source, int length)
    {
        var result = new uint[length];
        for (int i = 0; i < source.Length; i++) result[i] = source[i];
        return new(result);
    }
    
    public static SecureBigInteger Trim(SecureBigInteger source, int length) => Copy(source, 0, 0, length);
    public static SecureBigInteger Copy(SecureBigInteger source) => Copy(source, 0, 0, source.Length);

    public static SecureBigInteger OpTrim(SecureBigInteger source, int length)
    {
        var result = source._value.Take(length).ToArray();
        return new(result);
    }
}
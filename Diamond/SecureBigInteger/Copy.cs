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
        var minLen = Math.Min(source.Length, length);
        for (int i = 0; i < minLen; i++) result[i] = source[i];
        return new(result);
    }
    
    public static SecureBigInteger Trim(SecureBigInteger source, int length) => Copy(source, 0, 0, length);
    public static SecureBigInteger Copy(SecureBigInteger source) => Copy(source, 0, 0, source.Length);

    private static SecureBigInteger GetLow(SecureBigInteger x, int halfSize) => Pad(x, halfSize);
    private static SecureBigInteger GetHigh(SecureBigInteger x, int halfSize)
    {
        if (x.Length <= halfSize) return Zero;
        var result = new uint[x.Length - halfSize];
        CryptographicOperations.ConstantTime.Copy(x._value, halfSize, result, 0, result.Length);
        return new SecureBigInteger(result);
    }
}
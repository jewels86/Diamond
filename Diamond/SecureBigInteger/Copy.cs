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
    
    public static SecureBigInteger Trim(SecureBigInteger source, int length) => Copy(source, 0, 0, length);
    public static SecureBigInteger Copy(SecureBigInteger source) => Copy(source, 0, 0, source.Length);
}
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static uint operator >=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(a, b);
    public static uint operator <=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(b, a);
    public static uint operator ==(SecureBigInteger a, SecureBigInteger b) => Equal(a, b);
    public static uint operator !=(SecureBigInteger a, SecureBigInteger b) => NotEqual(a, b);
    public static uint operator >(SecureBigInteger a, SecureBigInteger b) => GreaterThan(a, b);
    public static uint operator <(SecureBigInteger a, SecureBigInteger b) => LessThan(a, b);
    
    public static uint GreaterThan(SecureBigInteger a, SecureBigInteger b) => CryptographicOperations.ConstantTime.Not(a <= b);
    public static uint LessThan(SecureBigInteger a, SecureBigInteger b) => CryptographicOperations.ConstantTime.Not(a >= b);
    
    public static uint IsEven(SecureBigInteger big) => CryptographicOperations.ConstantTime.IsZero(big[0] & 1);
    public static uint IsOdd(SecureBigInteger big) => CryptographicOperations.ConstantTime.Not(IsEven(big));


    public static uint GreaterThanOrEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var borrow = 0U;
        for (int i = 0; i < a.Length; i++)
        {
            var aVal = a.OpTryGetLimb(i, 0);
            var bVal = b.OpTryGetLimb(i, 0);
            var diff = (ulong)aVal - bVal - borrow;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        return 1U - borrow;
    }

    public static uint Equal(SecureBigInteger a, SecureBigInteger b)
    {
        var equal = 1U;
        for (int i = 0; i < a.Length; i++)
        {
            var aVal = a.OpTryGetLimb(i, 0);
            var bVal = b.OpTryGetLimb(i, 0);
            var isNonZero = CryptographicOperations.ConstantTime.IsNonZero(aVal - bVal);
            equal = CryptographicOperations.ConstantTime.Select(isNonZero, 0U, equal);
        }
        return equal;
    }

    public static uint NotEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var equal = Equal(a, b);
        return CryptographicOperations.ConstantTime.Not(equal);
    }
}
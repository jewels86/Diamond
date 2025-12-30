namespace Diamond;

public partial class SecureBigInteger
{
    public static uint GreaterThanOrEqual(SecureBigInteger a, SecureBigInteger b)
    {
        var maxLength = Math.Max(a.Length, b.Length);
        var paddedA = PadToLength(a, maxLength);
        var paddedB = PadToLength(b, maxLength);
        
        var borrow = 0U;
        for (int i = 0; i < a.Length; i++)
        {
            var diff = (ulong)paddedA[i] - paddedB[i] - borrow;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return 1U - borrow;
    }
    
    public static uint operator >=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(a, b);
    public static uint operator <=(SecureBigInteger a, SecureBigInteger b) => GreaterThanOrEqual(b, a);
}
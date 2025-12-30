namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator +(SecureBigInteger a, SecureBigInteger b) => Add(a, b);
    public static SecureBigInteger operator -(SecureBigInteger a, SecureBigInteger b) => Subtract(a, b);
    #region Addition
    public static SecureBigInteger Add(SecureBigInteger a, SecureBigInteger b)
    {
        var maxLength = Math.Max(a.Length, b.Length);
        var paddedA = PadToLength(a, maxLength);
        var paddedB = PadToLength(b, maxLength);
        
        var carry = 0U;
        var result = new uint[maxLength + 1];
        
        for (int i = 0; i < maxLength; i++)
        {
            var sum = (ulong)paddedA[i] + paddedB[i] + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        
        result[maxLength] = carry;
        
        return new SecureBigInteger(result);
    }
    
    public static SecureBigInteger Subtract(SecureBigInteger a, SecureBigInteger b)
    {
        var maxLength = Math.Max(a.Length, b.Length);
        var paddedA = PadToLength(a, maxLength);
        var paddedB = PadToLength(b, maxLength);
        
        var borrow = 0U;
        var result = new uint[maxLength];
        
        for (int i = 0; i < maxLength; i++)
        {
            var diff = (ulong)paddedA[i] - paddedB[i] - borrow;
            result[i] = (uint)diff;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return new SecureBigInteger(result);
    }
    #endregion
}
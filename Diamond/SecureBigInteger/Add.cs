namespace Diamond;

public partial class SecureBigInteger
{
    private static SecureBigInteger Add(SecureBigInteger a, SecureBigInteger b)
    {
        if (!LengthsMatch(a, b, out int length)) throw new ArgumentException("Cannot add two numbers of different lengths");
        
        var carry = 0U;
        var result = new uint[length];
        
        for (int i = 0; i < length; i++)
        {
            var sum = (ulong)a[i] + b[i] + carry;
            result[i] = (uint)sum;
            carry = CryptographicOperations.ConstantTime.ExtractUpperBits(sum);
        }
        
        return new SecureBigInteger(result);
    }
    
    private static SecureBigInteger Subtract(SecureBigInteger a, SecureBigInteger b)
    {
        if (!LengthsMatch(a, b, out int length)) throw new ArgumentException("Cannot add two numbers of different lengths");
        
        var borrow = 0U;
        var result = new uint[length];
        
        for (int i = 0; i < length; i++)
        {
            var diff = (ulong)a[i] - b[i] - borrow;
            result[i] = (uint)diff;
            borrow = CryptographicOperations.ConstantTime.ExtractOverflowBit(diff);
        }
        
        return new SecureBigInteger(result);
    }
}
namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ComputeBarrettMu(SecureBigInteger n)
    {
        var k = n.LogicalBitLength();
        var twoTo2k = One << 2 * k;
        return twoTo2k / n;
    }

    public static SecureBigInteger BarrettReduce(SecureBigInteger a, SecureBigInteger n, SecureBigInteger mu)
    {
        var k = n.LogicalBitLength();
        var q = a * mu >> 2 * k;
        var r = a - q * n;

        r = Select(r >= n, r - n, r);
        r = Select(r >= n, r - n, r);

        return Trim(r, n.Length); 
    }
    
    public static SecureBigInteger ModPowWithBarrett(SecureBigInteger baseValue, SecureBigInteger exponent, SecureBigInteger modulus, SecureBigInteger? mu = null)
    {
        mu ??= ComputeBarrettMu(modulus);
    
        var result = Copy(One);
        var baseBig = BarrettReduce(baseValue, modulus, mu);

        var expBits = GetBits(exponent);
    
        for (int i = 0; i < exponent.BitLength; i++)
        {
            var bit = expBits[i];
        
            var temp = result * baseBig;
            temp = BarrettReduce(temp, modulus, mu);
            result = Select(bit, temp, result);
        
            baseBig *= baseBig;
            baseBig = BarrettReduce(baseBig, modulus, mu);
        }
    
        return result;
    }
}
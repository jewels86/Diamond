namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ModPow(
        SecureBigInteger baseValue, 
        SecureBigInteger exponent, 
        SecureBigInteger modulus)
    {
        var mu = ComputeBarrettMu(modulus);
    
        var result = One;
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
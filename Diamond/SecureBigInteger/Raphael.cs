namespace Diamond;

public partial class SecureBigInteger
{
    public static (SecureBigInteger beta, int scale) ComputeRaphaelBeta(SecureBigInteger b, int aBitLength)
    {
        var k = b.LogicalBitLength() - 1;
        var lowerPower = One << k;
        var midpoint = lowerPower + (lowerPower >> 1);
        var usingHigher = b > midpoint;

        var m = Select(usingHigher, lowerPower << 1, lowerPower);
        k = (int)CryptographicOperations.ConstantTime.Select(usingHigher, (uint)k + 1, (uint)k);
        var n = Select(usingHigher, m - b, b - m);

        var N = Math.Max(2 + aBitLength - b.BitLength, 32);
        var s = Math.Max(aBitLength, 64);
        var h = n << s - k;

        var beta = One << s;
        beta = OpAOS(beta, h, 1u ^ usingHigher);
        
        var sLimbs = (s + 31) / 32;
        var hPow = h;
        for (int i = 2; i < N; i++)
        {
            hPow = OpMSrTSchoolbook(hPow, h, s, sLimbs); // Trim(hPow * h >> s, sLimbs)
            beta = OpAOS(beta, hPow, CryptographicOperations.ConstantTime.IsOdd((uint)i) & (1u ^ usingHigher)); // shouldSubtract ? Subtract(invB, hPow) : Add(invB, hPow)
        }

        var scale = s + k;
        return (beta, scale);
    }

    public static SecureBigInteger RaphaelDivide(SecureBigInteger a, SecureBigInteger b)
    {
        var (beta, scale) = ComputeRaphaelBeta(b, a.BitLength);
        return (a * beta) >> scale;
    }
    public static SecureBigInteger RaphaelReduce(SecureBigInteger a, SecureBigInteger n, SecureBigInteger beta, int scale)
    {
        var quotient = a * beta >> scale;
        var remainder = OpRemT(a, quotient, n, n.Length);

        return remainder;
    }

    public static SecureBigInteger ModPowWithRaphael(SecureBigInteger baseValue, SecureBigInteger exponent, SecureBigInteger modulus, SecureBigInteger? beta = null, int? scale = null)
    {
        if (beta is null || scale is null) (beta, scale) = ComputeRaphaelBeta(modulus, 2 * modulus.BitLength);

        var baseBig = RaphaelReduce(baseValue, modulus, beta, scale.Value);
        var result = Copy(One);
        
        var expBits = exponent.GetBits();
    
        for (int i = 0; i < exponent.BitLength; i++)
        {
            result = Select(expBits[i], RaphaelReduce(result * baseBig, modulus, beta, scale.Value), result);
            baseBig = RaphaelReduce(baseBig * baseBig, modulus, beta, scale.Value);
        }
    
        return result;
    }
}
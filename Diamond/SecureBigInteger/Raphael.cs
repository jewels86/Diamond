namespace Diamond;

public partial class SecureBigInteger
{
    public static (SecureBigInteger invB, int scale) ComputeRaphaelBeta(SecureBigInteger b, int aBitLength)
    {
        var k = b.LogicalBitLength() - 1;
        var lowerPower = One << k;
        var midpoint = lowerPower + (lowerPower >> 1);
        var usingHigher = b > midpoint;

        var m = Select(usingHigher, lowerPower << 1, lowerPower);
        k = (int)CryptographicOperations.ConstantTime.Select(usingHigher, (uint)k + 1, (uint)k);
        var n = Select(usingHigher, m - b, b - m);

        var N = Math.Max(aBitLength - b.BitLength, 32);
        var s = Math.Max(aBitLength, 64);
        
        var h = n << s - k;

        var invB = One << s;
        invB = OpAOS(invB, h, 1u ^ usingHigher);
        
        var sLimbs = (s + 31) / 32;
        var hPow = h;
        for (int i = 2; i < N; i++)
        {
            hPow = OpMSrT(hPow, h, s, sLimbs); // Trim(hPow * h >> s, sLimbs)
            invB = OpAOS(invB, hPow, CryptographicOperations.ConstantTime.IsOdd((uint)i) & (1u ^ usingHigher)); // shouldSubtract ? Subtract(invB, hPow) : Add(invB, hPow)
        }

        var scale = s + k;
        return (invB, scale);
    }

    public static SecureBigInteger RaphaelDivide(SecureBigInteger a, SecureBigInteger b) => RaphaelDivide(a, ComputeRaphaelBeta(b, a.BitLength));
    public static SecureBigInteger RaphaelDivide(SecureBigInteger a, (SecureBigInteger invB, int scale) beta) => a * beta.invB >> beta.scale;
    public static SecureBigInteger RaphaelReduce(SecureBigInteger a, SecureBigInteger n, (SecureBigInteger invB, int scale)? beta = null)
    {
        beta ??= ComputeRaphaelBeta(n, Math.Max(a.BitLength, 2 * n.BitLength));
    
        var quotient = (a * beta.Value.invB) >> beta.Value.scale;
        var remainder = a - quotient * n;

        return Trim(remainder, n.Length);
    }

    public static (SecureBigInteger quotient, SecureBigInteger remainder) RaphaelDivide(SecureBigInteger a, SecureBigInteger b, (SecureBigInteger invB, int scale) beta)
    {
        var product = a * beta.invB;

        var quotient = product >> beta.scale;
        var fractional = product & (One << beta.scale) - 1;
        var remainder = fractional * b >> beta.scale;

        return (quotient, remainder);
    }

    public static SecureBigInteger ModPowWithRaphael(SecureBigInteger baseValue, SecureBigInteger exponent, SecureBigInteger modulus, (SecureBigInteger invB, int scale)? beta = null)
    {
        beta ??= ComputeRaphaelBeta(modulus, 2 * modulus.BitLength);

        var baseBig = RaphaelReduce(baseValue, modulus, beta);
        var result = Copy(One);
        
        var expBits = exponent.GetBits();
    
        for (int i = 0; i < exponent.BitLength; i++)
        {
            result = Select(expBits[i], RaphaelReduce(result * baseBig, modulus, beta), result);
            baseBig = RaphaelReduce(baseBig * baseBig, modulus, beta);
        }
    
        return result;
    }
}
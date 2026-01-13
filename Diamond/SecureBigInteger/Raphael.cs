namespace Diamond;

public partial class SecureBigInteger
{
    public static RaphaelContext ComputeRaphaelBeta(SecureBigInteger b, int aBitLength)
    {
        var k = b.LogicalBitLength() - 1;
        var lowerPower = SafeLeftShift(One, k, b.BitLength);
        var midpoint = lowerPower + (lowerPower >> 1);
        var usingHigher = b > midpoint;

        var m = Select(usingHigher, lowerPower << 1, lowerPower);
        k = (int)CryptographicOperations.ConstantTime.Select(usingHigher, (uint)k + 1, (uint)k);
        var n = Select(usingHigher, m - b, b - m);

        var N = Math.Max(2 + aBitLength - b.BitLength, 32);
        var s = Math.Max(aBitLength, 64);
        var h = SafeLeftShift(n, s - k, b.BitLength + s);

        var beta = One << s;
        beta = OpAOS(beta, h, 1u ^ usingHigher);
        
        var sLimbs = (s + 31) / 32;
        var hPow = h;
        for (int i = 2; i < N; i++)
        {
            hPow = OpMSrTSchoolbook(hPow, h, s, sLimbs); // Trim(hPow * h >> s, sLimbs)
            beta = OpAOS(beta, hPow, CryptographicOperations.ConstantTime.IsOdd((uint)i) & (1u ^ usingHigher)); // shouldSubtract ? Subtract(invB, hPow) : Add(invB, hPow)
        }

        return new(beta, b, s, k);
    }
    
    public static SecureBigInteger RaphaelDivide(SecureBigInteger a, SecureBigInteger b) => RaphaelContext.RaphaelDivide(a, b);
    public static SecureBigInteger RaphaelReduce(SecureBigInteger a, SecureBigInteger n) => RaphaelContext.RaphaelReduce(a, n);

    public static SecureBigInteger ModPowWithRaphael(SecureBigInteger baseValue, SecureBigInteger exponent, SecureBigInteger modulus, RaphaelContext? ctx = null)
    {
        ctx ??= ComputeRaphaelBeta(modulus, 2 * modulus.BitLength);

        var baseBig = ctx.Reduce(baseValue);
        var result = Copy(One);
        
        var expBits = exponent.GetBits();
        for (int i = 0; i < exponent.BitLength; i++)
        {
            result = Select(expBits[i], ctx.Reduce(result * baseBig), result);
            baseBig = ctx.Reduce(baseBig * baseBig);
        }
    
        return result;
    }
}

public class RaphaelContext(SecureBigInteger beta, SecureBigInteger b, int s, int k)
{
    public SecureBigInteger Beta { get; } = beta;
    public SecureBigInteger B { get; } = b;
    public int S { get; } = s;
    public int K { get; } = k;

    public int Scale => S + K;
    public int MaxScale => S + B.BitLength;
    
    public SecureBigInteger Divide(SecureBigInteger a) => SecureBigInteger.RightShift(a * Beta, Scale);
    public SecureBigInteger Reduce(SecureBigInteger a)
    {
        var quotient = Divide(a);
        var remainder = SecureBigInteger.OpRemT(a, quotient, B, B.Length);

        return remainder;
    }

    public static SecureBigInteger RaphaelDivide(SecureBigInteger a, SecureBigInteger b)
    {
        var ctx = SecureBigInteger.ComputeRaphaelBeta(b, a.BitLength);
        return ctx.Divide(a);
    }

    public static SecureBigInteger RaphaelReduce(SecureBigInteger a, SecureBigInteger n)
    {
        var ctx = SecureBigInteger.ComputeRaphaelBeta(n, Math.Max(n.BitLength * 2, a.BitLength));
        return ctx.Reduce(a);
    }
}
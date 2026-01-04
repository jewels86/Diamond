using System.Diagnostics.CodeAnalysis;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ComputeNPrime(SecureBigInteger N, int k)
    {
        var n0 = N[0];

        var nPrime = 1UL;
        for (int bits = 2; bits <= 32; bits *= 2)
        {
            var mask = (1UL << bits) - 1;
            var temp = (n0 * nPrime) & mask;
            nPrime = (nPrime * ((2UL - temp) & mask)) & mask;
        }
        
        var limbCount = Math.Max(2, (k + 31) / 32);
        var result = new uint[limbCount];
        result[0] = (uint)nPrime;
    
        var needsSecondLimb = CryptographicOperations.ConstantTime.GreaterThan(limbCount, 1);
        result[1] = CryptographicOperations.ConstantTime.Select(needsSecondLimb, (uint)(nPrime >> 32), 0U);
        
        var nPrimeBig = new SecureBigInteger(result);
        var R = One << k;
    
        return (R - nPrimeBig) % R;
    }

    public static SecureBigInteger MontgomeryReduce(SecureBigInteger T, MontgomeryContext ctx)
    {
        var m = Copy(T);
    
        for (int i = 0; i < ctx.N.Length; i++)
        {
            var u_i = m[0] * ctx.NPrime[0];
            var uN = u_i * ctx.N;
            m += uN;
            m >>= 32;
        }
    
        m = Select(m >= ctx.N, m - ctx.N, m);
        return Trim(m, ctx.N.Length);
    }
    
    public static SecureBigInteger ModPowWithMontgomery(SecureBigInteger baseValue, SecureBigInteger exponent, SecureBigInteger modulus, MontgomeryContext? ctx = null)
    {
        ctx ??= new MontgomeryContext(modulus);
    
        var baseMont = ctx.ToMontgomery(baseValue);
        var resultMont = ctx.ToMontgomery(One);
    
        var expBits = GetBits(exponent);
    
        for (int i = 0; i < exponent.LogicalBitLength(); i++)
        {
            var bit = expBits[i];
        
            var temp = ctx.Multiply(resultMont, baseMont);
            resultMont = Select(bit, temp, resultMont);
            baseMont = ctx.Multiply(baseMont, baseMont);
        }
    
        return ctx.FromMontgomery(resultMont);
    }
}

public class MontgomeryContext
{
    public SecureBigInteger N { get; }
    public SecureBigInteger NPrime { get; }
    public SecureBigInteger R { get; }
    public int K { get; }

    public MontgomeryContext(SecureBigInteger n)
    {
        N = n;
        K = (n.LogicalBitLength() + 31) / 32 * 32;
        R = SecureBigInteger.One << K;
        NPrime = SecureBigInteger.ComputeNPrime(n, K);
    }
    
    public SecureBigInteger ToMontgomery(SecureBigInteger big) => big * R % N;
    public SecureBigInteger FromMontgomery(SecureBigInteger big) => SecureBigInteger.MontgomeryReduce(big, this);

    public SecureBigInteger Multiply(SecureBigInteger a, SecureBigInteger b) => SecureBigInteger.MontgomeryReduce(a * b, this);
}
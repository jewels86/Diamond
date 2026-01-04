using System.Diagnostics.CodeAnalysis;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ComputeNPrime(SecureBigInteger N, int k)
    {
        var n0 = N[0];
        
        var nPrime = 1UL;
        for (int bits = 2; bits <= 64; bits *= 2)
        {
            var mask = (1UL << bits) - 1;
            var temp = n0 * nPrime & mask;
            nPrime = nPrime * (2 - temp) & mask;
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
        var resultLength = T.Length + 1;
        var result = new uint[resultLength];
        
        CryptographicOperations.ConstantTime.Copy(T._value, 0, result, 0, T.Length);
    
        for (int i = 0; i < ctx.N.Length; i++)
        {
            var aLow = result[0];
            var u = aLow * ctx.NPrime[0];
        
            var carry = 0UL;
            for (int j = 0; j < ctx.N.Length; j++)
            {
                var product = (ulong)u * ctx.N[j];
                var sum = result[i + j] + product + carry;
                result[i + j] = (uint)sum;
                carry = sum >> 32;
            }
        
            result[i + ctx.N.Length] = (uint)carry;
        
            for (int k = 0; k < resultLength - 1; k++) result[k] = result[k + 1];
            result[resultLength - 1] = 0;
        }
    
        var resultBig = new SecureBigInteger(result);
        resultBig = Select(resultBig >= ctx.N, resultBig - ctx.N, resultBig);
        resultBig = Copy(resultBig, 0, 0, ctx.N.Length);

        return resultBig;
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
        K = (n.BitLength + 31) / 32 * 32;
        R = SecureBigInteger.One << K;
        NPrime = SecureBigInteger.ComputeNPrime(n, K);
    }
    
    public SecureBigInteger ToMontgomery(SecureBigInteger big) => big * R % N;
    public SecureBigInteger FromMontgomery(SecureBigInteger big) => SecureBigInteger.MontgomeryReduce(big, this);

    public SecureBigInteger Multiply(SecureBigInteger a, SecureBigInteger b) => SecureBigInteger.MontgomeryReduce(a * b, this);
}
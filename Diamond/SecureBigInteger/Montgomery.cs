using System.Diagnostics.CodeAnalysis;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ComputeNPrime(SecureBigInteger N, int k)
    {
        var hostN = N.AsHost();
        var n0 = hostN[0];
        
        var nPrime = 1UL;
        for (int bits = 2; bits <= 64; bits *= 2)
        {
            var mask = (1UL << bits) - 1;
            var temp = n0 * nPrime & mask;
            nPrime = nPrime * (2 - temp) & mask;
        }
        
        var limbCount = (k + 31) / 32;
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
        var hostT = T.AsHost();
        var (hostN, hostNPrime) = (ctx.N.AsHost(), ctx.NPrime.AsHost());
        var resultLength = hostT.Length + 1;
        var result = new uint[resultLength];
        
        CryptographicOperations.ConstantTime.Copy(hostT, 0, result, 0, hostT.Length);
    
        for (int i = 0; i < hostN.Length; i++)
        {
            var aLow = result[0];
            var u = aLow * hostNPrime[0];
        
            var carry = 0UL;
            for (int j = 0; j < hostN.Length; j++)
            {
                var product = (ulong)u * hostN[j];
                var sum = result[i + j] + product + carry;
                result[i + j] = (uint)sum;
                carry = sum >> 32;
            }
        
            result[i + hostN.Length] = (uint)carry;
        
            for (int k = 0; k < resultLength - 1; k++) result[k] = result[k + 1];
            result[resultLength - 1] = 0;
        }
    
        var resultBig = new SecureBigInteger(result);
        resultBig = Select(resultBig >= ctx.N, resultBig - ctx.N, resultBig);
        resultBig = Copy(resultBig, 0, 0, hostN.Length);

        return resultBig;
    }
}

public class MontgomeryContext : IDisposable
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


    public void Dispose()
    {
        N.Dispose();
        NPrime.Dispose();
        R.Dispose();
    }
}
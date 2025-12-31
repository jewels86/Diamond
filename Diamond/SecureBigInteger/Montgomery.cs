namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger REDC(SecureBigInteger T, MontgomeryContext ctx)
    {
        var workingLength = Math.Max(T.Length, ctx.N.Length + 1);
        var A = PadToLength(T, workingLength);
        var nPrimeLow = ctx.NPrime[0];

        for (int i = 0; i < ctx.N.Length; i++)
        {
            var aLow = A[0];
            var u = aLow * nPrimeLow & 0xFFFF_FFFF;
            
            var uBig = new SecureBigInteger([u]);
            var uN = uBig * ctx.N;
            A = A + uN >> 32;
        }
        
        var shouldSubtract = A >= ctx.N;
        return ConditionalSelect(shouldSubtract, A - ctx.N, A);
    }

    public static SecureBigInteger ComputeNPrime(SecureBigInteger N, int k)
    {
        var R = One << k;
        var NPrime = One;

        for (int bits = 2; bits <= k; bits++)
        {
            var modulus = One << bits;
            var temp = N * NPrime % modulus;
            var two = new SecureBigInteger([2]);
            NPrime = NPrime * (two - temp) % modulus;
        }

        return (R - NPrime) % R;
    }
}

public class MontgomeryContext : IDisposable
{
    public SecureBigInteger N { get; }
    public SecureBigInteger NPrime { get; }
    public SecureBigInteger R { get; } 
    public SecureBigInteger R2 { get; }
    public int K { get; }
    
    public MontgomeryContext(SecureBigInteger n)
    {
        N = n;
        K = (n.BitLength + 31) / 32 * 32;
        R = SecureBigInteger.One << K;
        R2 = R * R;
        NPrime = SecureBigInteger.ComputeNPrime(n, K);
    }

    public SecureBigInteger ToMontgomery(SecureBigInteger big) => SecureBigInteger.REDC(big * R2, this);
    public SecureBigInteger FromMontgomery(SecureBigInteger big) => SecureBigInteger.REDC(big, this);

    public SecureBigInteger Multiply(SecureBigInteger a, SecureBigInteger b) => SecureBigInteger.REDC(a * b, this);
    
    public void Dispose() { N.Dispose(); NPrime.Dispose(); R.Dispose(); }
}
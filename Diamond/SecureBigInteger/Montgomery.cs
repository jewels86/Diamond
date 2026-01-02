namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ComputeNPrime(SecureBigInteger N, int k)
    {
        var R = One << k;
        var NPrime = One;

        for (int bits = 2; bits <= k; bits++)
        {
            var modulus = One << bits;
            var temp = N * NPrime % modulus;
            NPrime = NPrime * (Two - temp) % modulus;
        }

        return (R - NPrime) % R;
    }
    
    // we'll do this later
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

    public void Dispose()
    {
        N.Dispose();
        NPrime.Dispose();
        R.Dispose();
    }
}
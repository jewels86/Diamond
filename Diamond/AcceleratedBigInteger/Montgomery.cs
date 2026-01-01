namespace Diamond;

public partial class AcceleratedBigInteger
{
    public static AcceleratedBigInteger ComputeNPrime(AcceleratedBigInteger N, int k)
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
}

public class MontgomeryContext : IDisposable
{
    public AcceleratedBigInteger N { get; }
    public AcceleratedBigInteger NPrime { get; }
    public AcceleratedBigInteger R { get; }
    public int K { get; }

    public MontgomeryContext(AcceleratedBigInteger n)
    {
        N = n;
        K = (n.BitLength + 31) / 32 * 32;
        R = AcceleratedBigInteger.One << K;
        NPrime = AcceleratedBigInteger.ComputeNPrime(n, K);
    }

    public void Dispose()
    {
        N.Dispose();
        NPrime.Dispose();
        R.Dispose();
    }
}
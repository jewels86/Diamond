namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger ComputeBarrettMu(SecureBigInteger n)
    {
        var k = n.BitLength;
        var twoTo2k = One << 2 * k;
        return twoTo2k / n;
    }

    public static SecureBigInteger BarrettReduce(SecureBigInteger a, SecureBigInteger n, SecureBigInteger mu)
    {
        var k = n.BitLength;
        var q = a * mu >> 2 * k;
        var r = a - q * n;

        r = Select(r >= n, r - n, r);
        r = Select(r >= n, r - n, r);

        return r;
    }
}
namespace Diamond;

public partial class AcceleratedBigInteger
{
    public static AcceleratedBigInteger ComputeBarrettMu(AcceleratedBigInteger n)
    {
        var k = n.BitLength;
        var twoTo2k = One << 2 * k;
        return twoTo2k / n;
    }

    public static AcceleratedBigInteger BarrettReduce(AcceleratedBigInteger a, AcceleratedBigInteger n, AcceleratedBigInteger mu)
    {
        var k = n.BitLength;
        var q = a * mu >> 2 * k;
        var r = a - q * n;

        r = ConditionalSelect(r >= n, r - n, r);
        r = ConditionalSelect(r >= n, r - n, r);

        return r;
    }
}
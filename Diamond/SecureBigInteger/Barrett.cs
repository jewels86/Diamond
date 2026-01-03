namespace Diamond;

public partial class SecureBigInteger
{
    // because *, >>, and - are all accelerated, barrett reduce is much faster than montgomery reduce
    // it's preferable to use barrett reduce for all modular arithmetic as % and REDC are host-based
    // ComputeBarrettMu will use / though, which is host-based, so mu will be on both the host and accelerated platforms
    // in terms of kernel launches, we do 3x2 (2 *'s) + 1 (1 >>) + 2x3 (3 -'s) + 2 (2 selects) + 2 (2 greater than's) = 17 kernel launches
    // we can optimize to fuse *>> and conditional subtracts (17 - 1 - 2 = 14 launches) later 
    
    public static SecureBigInteger ComputeBarrettMu(SecureBigInteger n)
    {
        var k = n.BitLength;
        var twoTo2k = One << 2 * k;
        return twoTo2k / n;
    }

    public static SecureBigInteger BarrettReduce(SecureBigInteger a, SecureBigInteger n, SecureBigInteger mu)
    {
        var k = n.BitLength;
        var q = a * mu >> 2 * k; // fuse this to single op later, just shift the bits in *'s final reduction kernel 
        var r = a - q * n;

        r = Select(r >= n, r - n, r); // we can make a conditional subtract, 3 kernels vs 4 so thats -2 total
        r = Select(r >= n, r - n, r);

        return r; 
    }
}
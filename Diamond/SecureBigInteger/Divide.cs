using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator /(SecureBigInteger a, SecureBigInteger b) => RaphaelDivide(a, b);
    public static SecureBigInteger operator %(SecureBigInteger a, SecureBigInteger b) => RaphaelReduce(a, b);

    public static (SecureBigInteger quotient, SecureBigInteger remainder) Divide(SecureBigInteger a, SecureBigInteger b)
    {
        var quotient = new SecureBigInteger(new uint[a.Length]);
        var remainder = new SecureBigInteger(new uint[a.Length + 1]);

        for (int bitPos = a.BitLength - 1; bitPos >= 0; bitPos--)
        {
            remainder <<= 1;

            var dividendBit = a.GetBit(bitPos);
            remainder[0] |= dividendBit;

            var canSubtract = remainder >= b;
            remainder = Select(canSubtract, remainder - b, remainder);

            var quotientLimb = bitPos / 32;
            var quotientBit = bitPos % 32;
            quotient[quotientLimb] |= canSubtract << quotientBit;
        }

        return (quotient, remainder);
    }

    
}
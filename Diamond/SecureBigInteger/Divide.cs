using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator /(SecureBigInteger a, SecureBigInteger b) => Divide(a, b).quotient;
    public static SecureBigInteger operator %(SecureBigInteger a, SecureBigInteger b) => Divide(a, b).remainder;

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

    public static (SecureBigInteger inverseB, int scale) ComputeRMInverseB(SecureBigInteger b, int aBitLength)
    {
        var k = b.LogicalBitLength() - 1;
        var lowerPower = One << k;
        var midpoint = lowerPower + (lowerPower >> 1);
        var usingHigher = b > midpoint;
        
        var m = Select(usingHigher, lowerPower << 1, lowerPower);
        k = (int)CryptographicOperations.ConstantTime.Select(usingHigher, (uint)k + 1, (uint)k);
        var n = Select(usingHigher, m - b, b - m);

        var N = Math.Max(aBitLength - b.BitLength, 32);
        var s = Math.Max(aBitLength, 64);
        var h = n << s - k;

        var invB = One << s;
        invB = OpAOS(invB, h, 1u ^ usingHigher);

        var sLimbs = (s + 31) / 32;
        var hPow = h;
        for (int i = 2; i < N; i++)
        {
            hPow = OpTMS(hPow, h, s, sLimbs); // Trim(hPow * h >> s, sLimbs)
            invB = OpAOS(invB, hPow, CryptographicOperations.ConstantTime.IsOdd((uint)i) & (1u ^ usingHigher)); // shouldSubtract ? Subtract(invB, hPow) : Add(invB, hPow)
        }

        var scale = s + k;
        return (invB, scale);
    }

    public static SecureBigInteger RMD(SecureBigInteger a, SecureBigInteger b)
    {
        var (invB, scale) = ComputeRMInverseB(b, a.BitLength);
        return (a * invB) >> scale;
    }
}
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

    public static (SecureBigInteger inverseB, int k) ComputeInverseB(SecureBigInteger b, int N = 30)
    {
        var k = b.LogicalBitLength();
        var n = Copy(b);
        n.ClearBit(k);

        var s = N * k;
        var h = n << s - k;

        var inverseBScaled = One << s;
        inverseBScaled -= h;

        var hPow = h;
        for (int i = 2; i < N; i++)
        {
            hPow = hPow * h >> s;
            if (CryptographicOperations.ConstantTime.IsEven((uint)i) == 1) inverseBScaled += hPow;
            else inverseBScaled -= hPow;
        }
        
        var totalScale = s + k;
        return (inverseBScaled, totalScale);
    }
}
using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static uint[] GetBits(SecureBigInteger big)
    {
        var result = new uint[big.BitLength];
        for (int i = 0; i < big.BitLength; i++) 
            result[i] = GetBit(big, i);
        
        return result;
    }

    public static uint GetBit(SecureBigInteger big, int i)
    {
        var limbIndex = i / 32;
        var bitPosition = i % 32;
        var limb = big[limbIndex];
        return limb >> bitPosition & 1u;
    }
}
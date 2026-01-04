using ILGPU;
using ILGPU.Runtime;
using Jewels.Lazulite;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger Select(uint condition, SecureBigInteger a, SecureBigInteger b)
    {
        var maxLen = Math.Max(a.Length, a.Length);
        var result = new uint[maxLen];

        for (int i = 0; i < maxLen; i++)
        {
            var aVal = a.TryGetLimb(i, 0);
            var bVal = b.TryGetLimb(i, 0);
            var selected = CryptographicOperations.ConstantTime.Select(condition, aVal, bVal);
            result[i] = selected;
        }
        
        return result;
    }
}
using System.Text;

namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger PadToLength(SecureBigInteger big, int targetLength)
    {
        if (big.Length >= targetLength) return big;

        var padded = new uint[targetLength];
        for (int i = 0; i < big.Length; i++) padded[i] = big[i];
        
        return new SecureBigInteger(padded);
    }
    
    public static SecureBigInteger TrimToLength(SecureBigInteger big, int targetLength)
    {
        if (big.Length <= targetLength) return big;
    
        var trimmed = new uint[targetLength];
        for (int i = 0; i < targetLength; i++) trimmed[i] = big[i];
    
        return new SecureBigInteger(trimmed);
    }

    public static SecureBigInteger ConditionalSelect(uint condition, SecureBigInteger a, SecureBigInteger b)
    {
        var maxLength = Math.Max(a.Length, b.Length);
        var paddedA = PadToLength(a, maxLength);
        var paddedB = PadToLength(b, maxLength);
        var result = new uint[maxLength];

        for (int i = 0; i < maxLength; i++)
            result[i] = CryptographicOperations.ConstantTime.Select(condition, paddedA[i], paddedB[i]);
        
        return new SecureBigInteger(result);
    }
    
    public override string ToString()
    {
        int highestNonZero = Length - 1;
        while (highestNonZero > 0 && this[highestNonZero] == 0) highestNonZero--;
    
        if (highestNonZero == 0 && this[0] == 0) return "0x0";
    
        var sb = new StringBuilder("0x");
        sb.Append(this[highestNonZero].ToString("X"));
        for (int i = highestNonZero - 1; i >= 0; i--) sb.Append(this[i].ToString("X8"));
    
        return sb.ToString();
    }
}
namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator <<(SecureBigInteger big, int bits) => LeftShift(big, bits);
    public static SecureBigInteger operator >>(SecureBigInteger big, int bits) => RightShift(big, bits);
    
    public static SecureBigInteger LeftShift(SecureBigInteger value, int bits)
    {
        var limbShift = bits / 32;
        var bitShift = bits % 32;
        
        var needsExtraLimb = bitShift > 0 ? 1 : 0;
        var result = new uint[value.Length + limbShift + needsExtraLimb];
    
        if (bitShift == 0) // we branch off of bit data, not the value in the limbs; constant time safe :)
        {
            for (int i = 0; i < value.Length; i++) result[i + limbShift] = value[i];
        }
        else
        {
            uint carry = 0;
            for (int i = 0; i < value.Length; i++)
            {
                var limb = value[i];
                result[i + limbShift] = limb << bitShift | carry;
                carry = limb >> 32 - bitShift;
            }
            result[value.Length + limbShift] = carry;
        }
    
        return new SecureBigInteger(result);
    }
    
    public static SecureBigInteger RightShift(SecureBigInteger value, int bits)
    {
        var limbShift = bits / 32;
        var bitShift = bits % 32;
    
        var resultLength = Math.Max(1, value.Length - limbShift);
    
        var paddedValue = PadToLength(value, value.Length + limbShift + 1);
        var result = new uint[resultLength];

        if (bitShift == 0)
        {
            for (int i = 0; i < resultLength; i++)
                result[i] = paddedValue[i + limbShift];
        }
        else
        {
            for (int i = 0; i < resultLength; i++)
            {
                var limb = paddedValue[i + limbShift];
                var nextLimb = paddedValue[i + limbShift + 1];
                result[i] = limb >> bitShift | nextLimb << 32 - bitShift;
            }
        }

        return new SecureBigInteger(result);
    }
}
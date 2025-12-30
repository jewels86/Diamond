namespace Diamond;

public partial class SecureBigInteger
{
    public static SecureBigInteger operator %(SecureBigInteger a, SecureBigInteger n) => Mod(a, n);
    
    public static SecureBigInteger Mod(SecureBigInteger a, SecureBigInteger n)
    {
        var maxLimbShift = Math.Max(0, (a.BitLength - n.BitLength) / 32 + 1);
        var workingLength = Math.Max(a.Length, n.Length) + maxLimbShift;
    
        var result = PadToLength(a, workingLength);
        var paddedN = PadToLength(n, workingLength);
    
        for (int limbShift = maxLimbShift; limbShift >= 0; limbShift--)
        {
            for (int bitShift = 31; bitShift >= 0; bitShift--)
            {
                var totalShift = limbShift * 32 + bitShift;
        
                var shiftedN = LeftShift(paddedN, totalShift);
                shiftedN = PadToLength(shiftedN, workingLength);
        
                var shiftIsValid = (uint)(totalShift <= a.BitLength - n.BitLength ? 1 : 0); // lengths and indices are public, so this is constant time 
                var resultIsGreater = GreaterThanOrEqual(result, shiftedN);
                var shouldSubtract = shiftIsValid & resultIsGreater;
        
                var subtracted = Subtract(result, shiftedN);
                result = ConditionalSelect(shouldSubtract, subtracted, result);
            }
        }
    
        return TrimToLength(result, n.Length);
    }
    // later we 
}
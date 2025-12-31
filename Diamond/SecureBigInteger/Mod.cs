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

    public static SecureBigInteger ModPow(SecureBigInteger a, SecureBigInteger exponent, MontgomeryContext ctx)
    {
        var r0 = ctx.ToMontgomery(One);
        var r1 = ctx.ToMontgomery(a);

        for (int i = exponent.BitLength - 1; i >= 0; i--)
        {
            var bit = exponent.GetBit(i);

            var r0Squared = ctx.Multiply(r0, r0);
            var r1Squared = ctx.Multiply(r1, r1);
            var r0r1 = ctx.Multiply(r0, r1);
            
            r0 = ConditionalSelect(bit, r0r1, r0Squared);
            r1 = ConditionalSelect(bit, r1Squared, r0r1);
        }
        
        return ctx.FromMontgomery(r0);
    }
}
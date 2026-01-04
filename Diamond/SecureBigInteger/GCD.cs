using System.Numerics;

namespace Diamond;

public partial class SecureBigInteger
{
    // !! this is NOT constant time with respect to input structure (trailing zeros leak) !!
    // because we shift based on the trailing zeros of inputs, the shift times will vary
    // this is unavoidable because that's just how shifts work- it'll take a different amount of time per shift amount no matter what
    // you can see this in our ConstantTime.Analytics.Tests.TestGCDEvenVsOdd test
    // thus please avoid using this on secret values! for most purposes, this is fine because GCD is used on values that aren't runtime secrets
    // but if you need it to be constant time, you'll need to use a different algorithm
    // reach out to me! :) we can reimplement with a different algorithm that's constant time
    public static SecureBigInteger GCD(SecureBigInteger a, SecureBigInteger b)
    {
        var x = Copy(a, 0, 0, a.Length);
        var y = Copy(b, 0, 0, b.Length);
    
        var xTrailing = CountTrailingZeros(x);
        var yTrailing = CountTrailingZeros(y);
        var commonShift = Math.Min(xTrailing, yTrailing);
    
        x >>= xTrailing;
        y >>= yTrailing;
    
        var maxIterations = (Math.Max(x.BitLength, y.BitLength) + 1) * 2;
    
        for (int i = 0; i < maxIterations; i++)
        {
            var xShifted = x >> CountTrailingZeros(x);
            x = Select(IsEven(x), xShifted, x);
    
            var yShifted = y >> CountTrailingZeros(y);
            y = Select(IsEven(y), yShifted, y);
    
            var equal = x == y;
            var xGreater = x > y;
            var diff = Select(xGreater, x - y, y - x);
    
            var newX = Select(xGreater, diff, x);
            var newY = Select(xGreater, y, diff);
    
            x = Select(equal, x, newX);
            y = Select(equal, y, newY);
        }
    
        var result = x << commonShift;
        
        result = Select(a == Zero, b, result);
        result = Select(b == Zero, a, result);
    
        return result;
    }

    public static int CountTrailingZeros(SecureBigInteger big) => 
        CryptographicOperations.ConstantTime.CountTrailingZeros(big._value);
}
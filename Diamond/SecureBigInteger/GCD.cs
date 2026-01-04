using System.Numerics;

namespace Diamond;

public partial class SecureBigInteger
{
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
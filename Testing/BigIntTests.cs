using Diamond;

namespace Testing;

public static class BigIntTests
{
    public static void TestAddAndSubtract()
    {
        Console.WriteLine("Testing Add and Subtract...");
        
        var a = new SecureBigInteger([34]);
        var b = new SecureBigInteger([34]);
        
        var c = a + b;
        var d = a - b;
        
        Console.WriteLine($"a: {a}, b: {b}, c: a + b = {c}, d: a - b = {d}");
        
        var e = new SecureBigInteger([uint.MaxValue, uint.MaxValue]);
        var f = new SecureBigInteger([uint.MaxValue, uint.MaxValue]);
        
        var g = e + f;
        var h = e - f;
        
        Console.WriteLine($"e: {e}, f: {f}, g: e + f = {g}, h: e - f = {h}");
        
        var i = new SecureBigInteger([uint.MaxValue]);
        var j = f + i;
        
        Console.WriteLine($"i: {i}, j: f + i = {j}");
    }

    public static void TestMultiply()
    {
        Console.WriteLine("Testing Multiply...");
        
        using var a = new SecureBigInteger([34]);
        using var b = new SecureBigInteger([34]);
        
        using var c = a * b;
        
        Console.WriteLine($"a: {a}, b: {b}, c: a * b = {c}");
        
        using var e = new SecureBigInteger([uint.MaxValue, uint.MaxValue]);
        using var f = new SecureBigInteger([uint.MaxValue, uint.MaxValue]);
        
        using var g = e * f;
        
        Console.WriteLine($"e: {e}, f: {f}, g: e * f = {g}");
        
        using var i = new SecureBigInteger([uint.MaxValue]);
        using var j = f * i;
        
        Console.WriteLine($"i: {i}, j: f * i = {j}");
    }

    public static void TestMod()
    {
        Console.WriteLine("Testing Mod...");
        
        using var a = new SecureBigInteger([5]);
        using var b = new SecureBigInteger([2]);
        
        using var c = a % b;
        
        Console.WriteLine($"a: {a}, b: {b}, c: a % b = {c}");
        
        using var e = new SecureBigInteger([uint.MaxValue, uint.MaxValue]);
        using var f = new SecureBigInteger([2]);
        
        using var g = e % f;
        
        Console.WriteLine($"e: {e}, f: {f}, g: e % f = {g}");
    }
}
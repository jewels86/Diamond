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

    public static void TestMonty()
    {
        Console.WriteLine("Testing Montgomery things...");

        using var ctx = new MontgomeryContext(101);
        
        using var big = new SecureBigInteger(100);
        using var asMonty = ctx.ToMontgomery(big);
        using var back = ctx.FromMontgomery(asMonty);
        using var bigModN = big % ctx.N;
        
        Console.WriteLine($"big: {big}, asMonty: {asMonty}, back: {back}, big mod N: {bigModN}");
        
        using var a = new SecureBigInteger([10]);
        using var aMonty = ctx.ToMontgomery(a);
        
        using var a2Monty = ctx.Multiply(aMonty, aMonty);
        using var a3Monty = ctx.Multiply(aMonty, a2Monty);
        using var a3 = ctx.FromMontgomery(a3Monty);
        
        using var computedA3ModN = (a * a * a) % ctx.N;
        
        Console.WriteLine($"a: {a}, aMonty: {aMonty}, a2Monty: {a2Monty}, a3Monty: {a3Monty}, a3: {a3}, a * a * a mod N: {computedA3ModN}");
    }

    public static void TestModPow()
    {
        Console.WriteLine("Testing ModPow...");
        using var ctx = new MontgomeryContext(101);

        var baseVal = new SecureBigInteger(5);
        var exp = new SecureBigInteger(3);
        var result = SecureBigInteger.ModPow(baseVal, exp, ctx);

        Console.WriteLine($"5^3 mod 101: {result}");
        
        var exp2 = new SecureBigInteger(100);
        var result2 = SecureBigInteger.ModPow(baseVal, exp2, ctx);
        Console.WriteLine($"5^100 mod 101: {result2}");
    }
}
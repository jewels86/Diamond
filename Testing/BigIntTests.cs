using System.Diagnostics;
using Diamond;

namespace Testing;

public static class BigIntTests
{
    private static readonly Random Random = new();
    
    #region Add/Subtract
    public static void TestAdd()
    {
        Console.WriteLine("Testing add...");
        
        // simple host case
        using var a1 = new SecureBigInteger(12);
        using var a2 = new SecureBigInteger(38);

        var sw = Stopwatch.StartNew();
        using var result = SecureBigInteger.HostAdd(a1, a2);
        sw.Stop();
        Console.WriteLine($"simple host add: a1: {a1}, a2: {a2}, result: {result} ({sw.ElapsedMilliseconds}ms)");
        
        // larger host case
        using var b1 = GenerateRandomBigInt(20);
        using var b2 = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        using var result2 = SecureBigInteger.HostAdd(b1, b2);
        sw.Stop();
        Console.WriteLine($"larger host add: b1: {b1}, b2: {b2}, result: {result2} ({sw.ElapsedMilliseconds}ms)");
        
        // simple accelerated case
        a1.ToAccelerated();
        a2.ToAccelerated();
        
        sw = Stopwatch.StartNew();
        using var result3 = SecureBigInteger.AcceleratedAdd(a1, a2);
        sw.Stop();
        Console.WriteLine($"simple accelerated add: a1: {a1}, a2: {a2}, result: {result3} ({sw.ElapsedMilliseconds}ms)");
        
        // larger accelerated case
        b1.ToAccelerated();
        b2.ToAccelerated();
        
        sw = Stopwatch.StartNew();
        using var result4 = SecureBigInteger.AcceleratedAdd(b1, b2);
        sw.Stop();
        Console.WriteLine($"larger accelerated add: b1: {b1}, b2: {b2}, result: {result4} ({sw.ElapsedMilliseconds}ms)");
    }

    public static void TestSubtract()
    {
        Console.WriteLine("Testing subtract...");
        
        // simple host case
        using var a1 = new SecureBigInteger(12);
        using var a2 = new SecureBigInteger(38);

        var sw = Stopwatch.StartNew();
        using var result = SecureBigInteger.HostSubtract(a1, a2);
        sw.Stop();
        Console.WriteLine($"simple host subtract: a1: {a1}, a2: {a2}, result: {result} ({sw.ElapsedMilliseconds}ms)");
        
        // larger host case
        using var b1 = GenerateRandomBigInt(20);
        using var b2 = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        using var result2 = SecureBigInteger.HostSubtract(b1, b2);
        sw.Stop();
        Console.WriteLine($"larger host subtract: b1: {b1}, b2: {b2}, result: {result2} ({sw.ElapsedMilliseconds}ms)");
        
        // simple accelerated case
        a1.ToAccelerated();
        a2.ToAccelerated();
        
        sw = Stopwatch.StartNew();
        using var result3 = SecureBigInteger.AcceleratedSubtract(a1, a2);
        sw.Stop();
        Console.WriteLine($"simple accelerated subtract: a1: {a1}, a2: {a2}, result: {result3} ({sw.ElapsedMilliseconds}ms)");
        
        // larger accelerated case
        b1.ToAccelerated();
        b2.ToAccelerated();
        
        sw = Stopwatch.StartNew();
        using var result4 = SecureBigInteger.AcceleratedSubtract(b1, b2);
        sw.Stop();
        Console.WriteLine($"larger accelerated subtract: b1: {b1}, b2: {b2}, result: {result4} ({sw.ElapsedMilliseconds}ms)");
    }
    #endregion
    #region Multiply
    public static void TestMultiply()
    {
        Console.WriteLine("Testing multiply...");

        // simple host case
        using var a1 = new SecureBigInteger(12);
        using var a2 = new SecureBigInteger(38);

        var sw = Stopwatch.StartNew();
        using var result = SecureBigInteger.HostMultiply(a1, a2);
        sw.Stop();
        Console.WriteLine($"simple host multiply: a1: {a1}, a2: {a2}, result: {result} ({sw.ElapsedMilliseconds}ms)");

        // larger host case
        using var b1 = GenerateRandomBigInt(20);
        using var b2 = GenerateRandomBigInt(10);

        sw = Stopwatch.StartNew();
        using var result2 = SecureBigInteger.HostMultiply(b1, b2);
        sw.Stop();
        Console.WriteLine($"larger host multiply: b1: {b1}, b2: {b2}, result: {result2} ({sw.ElapsedMilliseconds}ms)");
        
        // simple accelerated case
        a1.ToAccelerated();
        a2.ToAccelerated();
        
        sw = Stopwatch.StartNew();
        using var result3 = SecureBigInteger.AcceleratedMultiply(a1, a2);
        sw.Stop();
        Console.WriteLine($"simple accelerated multiply: a1: {a1}, a2: {a2}, result: {result3} ({sw.ElapsedMilliseconds}ms)");
        
        // larger accelerated case
        b1.ToAccelerated();
        b2.ToAccelerated();
        
        sw = Stopwatch.StartNew();
        using var result4 = SecureBigInteger.AcceleratedMultiply(b1, b2);
        sw.Stop();
        Console.WriteLine($"larger accelerated multiply: b1: {b1}, b2: {b2}, result: {result4} ({sw.ElapsedMilliseconds}ms)");
    }
    #endregion
    #region Monty
    public static void TestMonty()
    {
        var test1 = new SecureBigInteger(43);
        var test2 = new SecureBigInteger(0x100000000);
        var product2 = test1 * test2;
        Console.WriteLine($"43 * 2^32 = {product2}");
        
        var test3 = new SecureBigInteger(0x2b00000000);
        var test4 = new SecureBigInteger(12);
        var mod = test3 % test4;
        Console.WriteLine($"0x2b00000000 % 12 = {mod}");
        
        Console.WriteLine("Testing monty things...");

        using var n1 = new SecureBigInteger(12);
        using var ctx1 = new MontgomeryContext(n1);
        using var a1 = new SecureBigInteger(43);
        using var a2 = new SecureBigInteger(22);

        using var a1Mont = ctx1.ToMontgomery(a1);
        using var a2Mont = ctx1.ToMontgomery(a2);
        using var a1Back = ctx1.FromMontgomery(a1Mont);
        using var a2Back = ctx1.FromMontgomery(a2Mont);
        
        using var product = ctx1.Multiply(a1Mont, a2Mont);
        using var productBack = ctx1.FromMontgomery(product);
        using var productModN = a1 * a2 % n1;
        
        Console.WriteLine($"a1: {a1}, a2: {a2}, a1Mont: {a1Mont}, a2Mont: {a2Mont}, product: {product}, productBack: {productBack}, a1 * a2 % 12: {productModN}");
    }
    #endregion
    
    public static SecureBigInteger GenerateRandomBigInt(int wordCount)
    {
        uint[] result = new uint[wordCount];
        for (int i = 0; i < wordCount; i++) 
            result[i] = (uint)Random.Next() | (uint)Random.Next() << 31;
    
        if (result[wordCount - 1] == 0) result[wordCount - 1] = 1;
    
        return new SecureBigInteger(result);
    }
}
using System.Diagnostics;
using System.Numerics;
using Diamond;

namespace Testing;

public static class BigIntTests
{
    private static readonly Random Random = new();
    
    #region Add/Subtract
    public static void TestAdd()
    {
        var a = new SecureBigInteger(43);
        var b = new SecureBigInteger(12);
        
        var sw = Stopwatch.StartNew();
        var result = a + b;
        sw.Stop();
        Console.WriteLine($"simple addition: {a} + {b} = {result}, took {sw.ElapsedMilliseconds}ms");

        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = a + b;
        sw.Stop();
        Console.WriteLine($"larger addition: {a} + {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(1000);
        b = GenerateRandomBigInt(1000);
        
        sw = Stopwatch.StartNew();
        result = a + b;
        sw.Stop();
        Console.WriteLine($"huge addition with size {a.Length} + size {b.Length} took {sw.ElapsedMilliseconds}ms");
    }

    public static void TestSubtract()
    {
        var a = new SecureBigInteger(43);
        var b = new SecureBigInteger(12);
        
        var sw = Stopwatch.StartNew();
        var result = a - b;
        sw.Stop();
        Console.WriteLine($"simple subtraction: {a} - {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = a - b;
        sw.Stop();
        Console.WriteLine($"larger subtraction: {a} - {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(1000);
        b = GenerateRandomBigInt(1000);
        
        sw = Stopwatch.StartNew();
        result = a - b;
        sw.Stop();
        Console.WriteLine($"huge subtraction with size {a.Length} - size {b.Length} took {sw.ElapsedMilliseconds}ms");
    }
    #endregion
    #region Multiply/Divide/Mod
    public static void TestMultiply()
    {
        var a = new SecureBigInteger(43);
        var b = new SecureBigInteger(12);
        
        var sw = Stopwatch.StartNew();
        var result = a * b;
        sw.Stop();
        Console.WriteLine($"simple multiplication: {a} * {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = a * b;
        sw.Stop();
        Console.WriteLine($"larger multiplication: {a} * {b} = {result}, took {sw.ElapsedMilliseconds}ms");

        a = GenerateRandomBigInt(128);
        b = GenerateRandomBigInt(128);
        
        sw = Stopwatch.StartNew();
        result = a * b;
        sw.Stop();
        Console.WriteLine($"big multiplication with size {a.Length} * size {b.Length} took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(1000);
        b = GenerateRandomBigInt(1000);
        
        sw = Stopwatch.StartNew();
        result = a * b;
        sw.Stop();
        Console.WriteLine($"huge multiplication with size {a.Length} * size {b.Length} took {sw.ElapsedMilliseconds}ms");
    }

    public static void TestDivide()
    {
        var a = new SecureBigInteger(43);
        var b = new SecureBigInteger(12);
        
        var sw = Stopwatch.StartNew();
        var result = a / b;
        sw.Stop();
        Console.WriteLine($"simple division: {a} / {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = a / b;
        sw.Stop();
        Console.WriteLine($"larger division: {a} / {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(128);
        b = GenerateRandomBigInt(128);
        
        sw = Stopwatch.StartNew();
        result = a / b;
        sw.Stop();
        Console.WriteLine($"big division with size {a.Length} / size {b.Length} took {sw.ElapsedMilliseconds}ms");
    }

    public static void TestMod()
    {
        var a = new SecureBigInteger(43);
        var b = new SecureBigInteger(12);
        
        var sw = Stopwatch.StartNew();
        var result = a % b;
        sw.Stop();
        Console.WriteLine($"simple modulus: {a} % {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = a % b;
        sw.Stop();
        Console.WriteLine($"larger modulus: {a} % {b} = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(128);
        b = GenerateRandomBigInt(128);
        
        sw = Stopwatch.StartNew();
        result = a % b;
        sw.Stop();
        Console.WriteLine($"big modulus with size {a.Length} % size {b.Length} took {sw.ElapsedMilliseconds}ms");
    }
    #endregion
    #region GCD
    public static void TestGCD()
    {
        var a = new SecureBigInteger(48);
        var b = new SecureBigInteger(18);

        var sw = Stopwatch.StartNew();
        var result = SecureBigInteger.GCD(a, b);
        sw.Stop();
        Console.WriteLine($"GCD({a}, {b}) = {result}, took {sw.ElapsedMilliseconds}ms");

        a = new SecureBigInteger(101);
        b = new SecureBigInteger(103);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.GCD(a, b);
        sw.Stop();
        Console.WriteLine($"GCD({a}, {b}) = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.GCD(a, b);
        sw.Stop();
        Console.WriteLine($"GCD({a}, {b}) = {result}, took {sw.ElapsedMilliseconds}ms");
    }

    public static void TestModInverse()
    {
        var a = new SecureBigInteger(3);
        var b = new SecureBigInteger(11);
        
        var sw = Stopwatch.StartNew();
        var result = SecureBigInteger.ModInverse(a, b);
        sw.Stop();
        Console.WriteLine($"ModInverse({a}, {b}) = {result}, took {sw.ElapsedMilliseconds}ms");

        a = new SecureBigInteger(10);
        b = new SecureBigInteger(15);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModInverse(a, b);
        sw.Stop();
        Console.WriteLine($"ModInverse({a}, {b}) = {result}, took {sw.ElapsedMilliseconds}ms");
        
        a = GenerateRandomBigInt(20);
        b = GenerateRandomBigInt(10);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModInverse(a, b);
        sw.Stop();
        Console.WriteLine($"ModInverse({a}, {b}) = {result}, took {sw.ElapsedMilliseconds}ms");
    }
    #endregion
    #region Barrett
    public static void TestBarrett()
    {
        var baseBig = new SecureBigInteger(7);
        var exponent = new SecureBigInteger(3);
        var modulus = new SecureBigInteger(13);
        
        var sw = Stopwatch.StartNew();
        var result = SecureBigInteger.ModPowWithBarrett(baseBig, exponent, modulus);
        sw.Stop();
        Console.WriteLine($"ModPow({baseBig}, {exponent}, {modulus}) = {result}, expected 0x5 ({sw.ElapsedMilliseconds}ms)");

        baseBig = new SecureBigInteger(5);
        exponent = new SecureBigInteger(10);
        modulus = new SecureBigInteger(221);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModPowWithBarrett(baseBig, exponent, modulus);
        sw.Stop();
        Console.WriteLine($"ModPow({baseBig}, {exponent}, {modulus}) = {result}, expected {BigInteger.ModPow(5, 10, 221)} ({sw.ElapsedMilliseconds}ms)");
    }
    #endregion
    #region Monty
    public static void TestMonty()
    {
        var n = new SecureBigInteger(13);
        var ctx = new MontgomeryContext(n);

        Console.WriteLine($"N = {n}");
        Console.WriteLine($"K = {ctx.K}");
        Console.WriteLine($"R = {ctx.R}");
        Console.WriteLine($"N' = {ctx.NPrime}");
        
        var a = new SecureBigInteger(7);
        var aMont = ctx.ToMontgomery(a);
        var aBack = ctx.FromMontgomery(aMont);
        Console.WriteLine($"a = {a}, in Montgomery = {aMont}, back = {aBack}");

        var b = new SecureBigInteger(5);
        var bMont = ctx.ToMontgomery(b);
        var resultMont = ctx.Multiply(aMont, bMont);
        var result = ctx.FromMontgomery(resultMont);
        Console.WriteLine($"{a} * {b} mod {n} = {result}, expected 0x9");
        
        var baseBig = new SecureBigInteger(7);
        var exponent = new SecureBigInteger(3);
        var modulus = new SecureBigInteger(13);

        var sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModPowWithMontgomery(baseBig, exponent, modulus);
        sw.Stop();
        Console.WriteLine($"ModPow({baseBig}, {exponent}, {modulus}) = {result}, expected 0x5 ({sw.ElapsedMilliseconds}ms)");

        baseBig = new SecureBigInteger(5);
        exponent = new SecureBigInteger(10);
        modulus = new SecureBigInteger(221);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModPowWithMontgomery(baseBig, exponent, modulus);
        sw.Stop();
        Console.WriteLine($"ModPow({baseBig}, {exponent}, {modulus}) = {result}, expected {BigInteger.ModPow(5, 10, 221)} ({sw.ElapsedMilliseconds}ms)");
    }
    #endregion
    #region Raphael
    public static void TestRaphael()
    {
        var sw = Stopwatch.StartNew();
        var inverse27 = SecureBigInteger.RaphaelDivide(531, 27);
        sw.Stop();
        Console.WriteLine($"RM(531, 27) = {inverse27} ({sw.ElapsedMilliseconds}ms)");

        sw = Stopwatch.StartNew();
        var largeDivision = SecureBigInteger.RaphaelDivide(new SecureBigInteger(2311567), new SecureBigInteger(14000));
        sw.Stop();
        Console.WriteLine($"RM(2311567, 14000) = {largeDivision} ({sw.ElapsedMilliseconds}ms)");
        
        var random1 = GenerateRandomBigInt(128);
        var random2 = GenerateRandomBigInt(64).PadTo(128);
        var expected = ToPositiveBigInteger(random1) / ToPositiveBigInteger(random2);
        sw = Stopwatch.StartNew();
        var result = SecureBigInteger.RaphaelDivide(random1, random2);
        sw.Stop();
        var raphaelTime = sw.ElapsedMilliseconds;
        sw = Stopwatch.StartNew();
        SecureBigInteger.LongDivide(random1, random2);                     
        sw.Stop();
        
        Console.WriteLine($"Our result:\t {ToPositiveBigInteger(result)}");
        Console.WriteLine($"Expected:\t {expected}");
        Console.WriteLine($"RM([{random1.Length} size], [{random2.Length} size]) took {raphaelTime}ms vs long divide's {sw.ElapsedMilliseconds}");
        
        var baseBig = new SecureBigInteger(7);
        var exponent = new SecureBigInteger(3);
        var modulus = new SecureBigInteger(13);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModPowWithRaphael(baseBig, exponent, modulus);
        sw.Stop();
        Console.WriteLine($"ModPow({baseBig}, {exponent}, {modulus}) = {result}, expected 0x5 ({sw.ElapsedMilliseconds}ms)");

        baseBig = new SecureBigInteger(5);
        exponent = new SecureBigInteger(10);
        modulus = new SecureBigInteger(221);
        
        sw = Stopwatch.StartNew();
        result = SecureBigInteger.ModPowWithRaphael(baseBig, exponent, modulus);
        sw.Stop();
        Console.WriteLine($"ModPow({baseBig}, {exponent}, {modulus}) = {result}, expected {BigInteger.ModPow(5, 10, 221)} ({sw.ElapsedMilliseconds}ms)");
    }
    #endregion

    #region Stupid
    public static void TestStupids()
    {
        var b = new SecureBigInteger(BigInteger.Parse("123456789012345678901234567890123456789012345678901234567890"));
        var e = new SecureBigInteger(65537);
        var n = new SecureBigInteger(BigInteger.Parse("987654321098765432109876543210987654321098765432109876543211"));

        var sw = Stopwatch.StartNew();
        var resultBarrett = SecureBigInteger.ModPowWithBarrett(b, e, n);
        sw.Stop();
        var barrettTime = sw.ElapsedMilliseconds;
        
        sw = Stopwatch.StartNew();
        var resultMontgomery = SecureBigInteger.ModPowWithMontgomery(b, e, n);
        sw.Stop();
        var montgomeryTime = sw.ElapsedMilliseconds;
        
        sw = Stopwatch.StartNew();
        var resultRaphael = SecureBigInteger.ModPowWithRaphael(b, e, n);
        sw.Stop();
        var raphaelTime = sw.ElapsedMilliseconds;
        
        
        var expected = BigInteger.ModPow(
            BigInteger.Parse("123456789012345678901234567890123456789012345678901234567890"),
            65537,
            BigInteger.Parse("987654321098765432109876543210987654321098765432109876543211")
        );

        Console.WriteLine($"Barrett (decimal):    {new BigInteger(resultBarrett.ToBytes())} ({barrettTime}ms)");
        Console.WriteLine($"Montgomery (decimal): {new BigInteger(resultMontgomery.ToBytes())} ({montgomeryTime}ms)");
        Console.WriteLine($"Raphael (decimal):    {new BigInteger(resultRaphael.ToBytes())} ({raphaelTime}ms)");
        Console.WriteLine($"Expected (decimal):   {expected}");
    }
    public static void TestStupids2()
    {
        var b = new SecureBigInteger(BigInteger.Parse("123456789012345678901234567890123456789012345678901234567890"));
        var e = new SecureBigInteger(65537);
        var n = new SecureBigInteger(BigInteger.Parse("987654321098765432109876543210987654321098765432109876543211"));

        var mu = SecureBigInteger.ComputeBarrettMu(n);
        var sw = Stopwatch.StartNew();
        var resultBarrett = SecureBigInteger.ModPowWithBarrett(b, e, n, mu);
        sw.Stop();
        var barrettTime = sw.ElapsedMilliseconds;
        
        var ctx = new MontgomeryContext(n);
        sw = Stopwatch.StartNew();
        var resultMontgomery = SecureBigInteger.ModPowWithMontgomery(b, e, n, ctx);
        sw.Stop();
        var montgomeryTime = sw.ElapsedMilliseconds;
        
        var raphaelCTX = SecureBigInteger.ComputeRaphaelBeta(n, n.BitLength * 2);
        sw = Stopwatch.StartNew();
        var resultRaphael = SecureBigInteger.ModPowWithRaphael(b, e, n, raphaelCTX);
        sw.Stop();
        var raphaelTime = sw.ElapsedMilliseconds;
        
        var expected = BigInteger.ModPow(
            BigInteger.Parse("123456789012345678901234567890123456789012345678901234567890"),
            65537,
            BigInteger.Parse("987654321098765432109876543210987654321098765432109876543211")
        );

        Console.WriteLine($"Barrett (decimal):    {new BigInteger(resultBarrett.ToBytes())} ({barrettTime}ms)");
        Console.WriteLine($"Montgomery (decimal): {new BigInteger(resultMontgomery.ToBytes())} ({montgomeryTime}ms)");
        Console.WriteLine($"Raphael (decimal):    {new BigInteger(resultRaphael.ToBytes())} ({raphaelTime}ms)");
        Console.WriteLine($"Expected (decimal):   {expected}");
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
    
    public static BigInteger ToPositiveBigInteger(SecureBigInteger sbi)
    {
        var bytes = sbi.ToBytes();
        if (bytes[^1] < 0x80) return new BigInteger(bytes);
        
        var newBytes = new byte[bytes.Length + 1];
        Array.Copy(bytes, newBytes, bytes.Length);
        newBytes[bytes.Length] = 0;
        return new BigInteger(newBytes);
    }
}
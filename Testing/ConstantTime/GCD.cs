using Diamond;

namespace Testing.ConstantTime;

public static partial class Analytics
{
    public static void TestGCD()
    {
        Console.WriteLine("Testing GCD with coprime and non-coprime inputs...");
        var random = new Random();
        const int wordCount = 4;
        const int warmup = 1000;
        const int iterationsMultiplier = 1;

        Console.WriteLine($"Generating {warmup} random number pairs...");
        
        List<SecureBigInteger> coprimeA = [];
        List<SecureBigInteger> coprimeB = [];
        List<SecureBigInteger> nonCoprimeA = [];
        List<SecureBigInteger> nonCoprimeB = [];
        
        for (int i = 0; i < warmup; i++)
        {
            var a = GenerateRandomBigInt(wordCount, random) | SecureBigInteger.One;
            var b = GenerateRandomBigInt(wordCount, random) | SecureBigInteger.One;
            coprimeA.Add(a);
            coprimeB.Add(b);
            
            var commonFactor = GenerateRandomBigInt(2, random) | SecureBigInteger.One;
            var x = GenerateRandomBigInt(wordCount - 2, random) | SecureBigInteger.One;
            var y = GenerateRandomBigInt(wordCount - 2, random) | SecureBigInteger.One;
            nonCoprimeA.Add(x * commonFactor);
            nonCoprimeB.Add(y * commonFactor);
        }

        Console.WriteLine("Warming up...");
        for (int i = 0; i < warmup; i++)
        {
            SecureBigInteger.GCD(coprimeA[i], coprimeB[i]);
            SecureBigInteger.GCD(nonCoprimeA[i], nonCoprimeB[i]);
        }

        List<long> coprimeTimes = [];
        List<long> nonCoprimeTimes = [];

        var samples = iterationsMultiplier * warmup;
        Console.WriteLine($"Running {samples} samples...");
        
        for (int i = 0; i < samples; i++)
        {
            var sample = i % warmup;
            coprimeTimes.Add(TimeOperation(() => SecureBigInteger.GCD(coprimeA[sample], coprimeB[sample])));
            nonCoprimeTimes.Add(TimeOperation(() => SecureBigInteger.GCD(nonCoprimeA[sample], nonCoprimeB[sample])));
        }
        
        AnalyzeResults("GCD Coprime vs Non-Coprime", coprimeTimes, nonCoprimeTimes);
    }

    public static void TestGCDEvenVsOdd()
    {
        Console.WriteLine("Testing GCD with even vs odd inputs...");
        var random = new Random();
        const int wordCount = 4;
        const int warmup = 1000;
        const int iterationsMultiplier = 1;

        List<SecureBigInteger> oddA = [];
        List<SecureBigInteger> oddB = [];
        List<SecureBigInteger> evenA = [];
        List<SecureBigInteger> evenB = [];

        for (int i = 0; i < warmup; i++)
        {
            oddA.Add(GenerateRandomBigInt(wordCount, random) | SecureBigInteger.One);
            oddB.Add(GenerateRandomBigInt(wordCount, random) | SecureBigInteger.One);

            var shiftAmount = random.Next(1, 10);
            evenA.Add(GenerateRandomBigInt(wordCount, random) << shiftAmount);
            evenB.Add(GenerateRandomBigInt(wordCount, random) << shiftAmount);
        }
        
        Console.WriteLine("Warming up...");
        for (int i = 0; i < warmup; i++)
        {
            SecureBigInteger.GCD(oddA[i], oddB[i]);
            SecureBigInteger.GCD(evenA[i], evenB[i]);
        }
        
        List<long> evenTimes = [];
        List<long> oddTimes = [];

        var samples = iterationsMultiplier * warmup;
        Console.WriteLine($"Running {samples} samples...");
        
        for (int i = 0; i < samples; i++)
        {
            var sample = i % warmup;
            evenTimes.Add(TimeOperation(() => SecureBigInteger.GCD(evenA[sample], evenB[sample])));
            oddTimes.Add(TimeOperation(() => SecureBigInteger.GCD(oddA[sample], oddB[sample])));
        }
        
        AnalyzeResults("GCD Even vs Odd", evenTimes, oddTimes);
    }
}
using Diamond;

namespace Testing.ConstantTime;

public partial class Analytics
{
    public static void TestRaphaelDivide()
    {
        Console.WriteLine("Testing Raphael's Method...");
        const int wordCountSmall = 64;
        const int wordCountLarge = 128;
        const int warmup = 100;
        const int iterationsMultiplier = 3;

        Console.WriteLine($"Generating {warmup} random divisors...");
        List<SecureBigInteger> smallDivisors = [];
        List<SecureBigInteger> largeDivisors = [];
        var numerator = GenerateRandomBigInt(128);

        for (int i = 0; i < warmup; i++)
        {
            smallDivisors.Add(SecureBigInteger.Pad(GenerateRandomBigInt(wordCountSmall), wordCountLarge));
            largeDivisors.Add(GenerateRandomBigInt(wordCountLarge));
        }

        Console.WriteLine("Warming up...");
        for (int i = 0; i < warmup; i++)
        {
            SecureBigInteger.RaphaelDivide(numerator, largeDivisors[i]);
            SecureBigInteger.RaphaelDivide(numerator, smallDivisors[i]);
        }

        List<long> smallTimes = [];
        List<long> largeTimes = [];

        var samples = iterationsMultiplier * warmup;
        Console.WriteLine($"Running {samples} samples...");
        
        for (int i = 0; i < samples; i++)
        {
            var sample = i % warmup;
            smallTimes.Add(TimeOperation(() => SecureBigInteger.RaphaelDivide(numerator, smallDivisors[sample])));
            largeTimes.Add(TimeOperation(() => SecureBigInteger.RaphaelDivide(numerator, largeDivisors[sample])));
        }
        
        AnalyzeResults("Raphael's method small vs large results", smallTimes, largeTimes);
    }
}
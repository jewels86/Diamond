using Diamond;

namespace Testing.ConstantTime;

public static partial class Analytics
{
    public static void TestMontgomeryModPow()
    {
        Console.WriteLine("Testing ModPow with Montgomery Exponents...");
        var random = new Random();
        const int wordCount = 4;
        const int warmup = 100;
        const int iterationsMultiplier = 1;

        Console.WriteLine($"Generating {warmup} random exponents...");
        var modulus = GenerateRandomBigInt(wordCount, random);
        var baseValue = GenerateRandomBigInt(wordCount, random);
        var ctx = new MontgomeryContext(modulus);

        var sparseExponents = GenerateRandomSetWithHammingWeight(warmup, wordCount, 0.9f);
        var denseExponents = GenerateRandomSetWithHammingWeight(warmup, wordCount, 0.1f);

        Console.WriteLine("Warming up...");
        for (int i = 0; i < warmup; i++)
        {
            SecureBigInteger.ModPowWithMontgomery(baseValue, sparseExponents[i], modulus, ctx);
            SecureBigInteger.ModPowWithMontgomery(baseValue, denseExponents[i], modulus, ctx);
        }

        List<long> sparseTimes = [];
        List<long> denseTimes = [];

        var samples = iterationsMultiplier * warmup;
        Console.WriteLine($"Running {samples} samples...");
        
        for (int i = 0; i < samples; i++)
        {
            var sample = i % warmup;
            sparseTimes.Add(TimeOperation(() => SecureBigInteger.ModPowWithMontgomery(baseValue, sparseExponents[sample], modulus, ctx)));
            denseTimes.Add(TimeOperation(() => SecureBigInteger.ModPowWithMontgomery(baseValue, denseExponents[sample], modulus, ctx)));
        }
        
        AnalyzeResults("ModPow with Montgomery Sparse v. Dense Results", sparseTimes, denseTimes);
    }
}
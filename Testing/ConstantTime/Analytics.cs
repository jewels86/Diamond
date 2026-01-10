using System.Diagnostics;
using System.Security.Cryptography;
using Diamond;

namespace Testing.ConstantTime;

public static partial class Analytics
{
    public static RandomNumberGenerator RNG = RandomNumberGenerator.Create();
    public readonly static Random Random = new(42);
    
    public static long TimeOperation(Action operation)
    {
        var sw = Stopwatch.StartNew();
        operation();
        sw.Stop();
        return sw.ElapsedTicks;
    }
    
    public static void AnalyzeResults(string testName, List<long> setA, List<long> setB)
    {
        var meanA = setA.Average();
        var meanB = setB.Average();
        var stdDevA = Math.Sqrt(setA.Average(v => Math.Pow(v - meanA, 2)));
        var stdDevB = Math.Sqrt(setB.Average(v => Math.Pow(v - meanB, 2)));
        
        Console.WriteLine($"{testName}:");
        Console.WriteLine($"Set A: Mean = {meanA:F2}, StdDev = {stdDevA:F2}");
        Console.WriteLine($"Set B: Mean = {meanB:F2}, StdDev = {stdDevB:F2}");
        Console.WriteLine($"Difference: {Math.Abs(meanA - meanB):F2} ({Math.Abs(meanA - meanB) / Math.Max(meanA, meanB) * 100:F2}%)");
        
        var tStat = (meanA - meanB) / Math.Sqrt((stdDevA * stdDevA / setA.Count) + (stdDevB * stdDevB / setB.Count));
        Console.WriteLine($"t-statistic: {Math.Abs(tStat):F4}");
        Console.WriteLine(Math.Abs(tStat) < 2.0 ? "[*] No significant difference detected" : "[!] Possible timing difference!");
    }
    
    public static SecureBigInteger GenerateRandomBigInt(int wordCount, Random? random = null)
    {
        random ??= Random;
        uint[] result = new uint[wordCount];
        for (int i = 0; i < wordCount; i++) 
            result[i] = (uint)random.Next() | (uint)random.Next() << 31;
    
        if (result[wordCount - 1] == 0) result[wordCount - 1] = 1;
    
        return new SecureBigInteger(result);
    }

    public static List<SecureBigInteger> GenerateRandomSetWithHammingWeight(int total, int wordCount, float hammingWeight)
    {
        var result = new List<SecureBigInteger>();
        var rng = RandomNumberGenerator.Create();
    
        var totalBits = wordCount * 32;
        var targetOnes = (int)(totalBits * hammingWeight);
    
        for (int i = 0; i < total; i++)
        {
            var bytes = new byte[(wordCount * 4)];
        
            Array.Clear(bytes, 0, bytes.Length);
            HashSet<int> setBits = [];
        
            while (setBits.Count < targetOnes)
            {
                var randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                var bitPosition = (int)(BitConverter.ToUInt32(randomBytes, 0) % totalBits);

                if (!setBits.Add(bitPosition)) continue;
                
                int byteIndex = bitPosition / 8;
                int bitIndex = bitPosition % 8;
                bytes[byteIndex] |= (byte)(1 << bitIndex);
            }
        
            result.Add(new(bytes));
        }
    
        return result;
    }
}

using System.Diagnostics;
using Diamond;
using static Diamond.Hashing;

namespace Testing;

public class HashingTests
{
    public static void TestSHA256()
    {
        var results = AcceleratedSHA256Hex(["abc", "", "hello world"]);
        
        Console.WriteLine($"abc -> {SHA256Hex("abc")}, accelerated {results[0]}");
        Console.WriteLine($"'' -> {SHA256Hex("")}, accelerated {results[1]}");
        Console.WriteLine($"hello world -> {SHA256Hex("hello world")}, accelerated {results[2]}");

        int n = 80000;
        string[] messages = new string[n];
        for (int i = 0; i < n; i++) messages[i] = Guid.NewGuid().ToString();
        
        Stopwatch sw = Stopwatch.StartNew();
        var acceleratedResults = AcceleratedSHA256Hex(messages);
        sw.Stop();
        Console.WriteLine($"Accelerated SHA256 took {sw.ElapsedMilliseconds}ms to hash {n} messages");
        
        sw = Stopwatch.StartNew();
        var regularResults = messages.Select(SHA256Hex).ToArray();
        sw.Stop();
        Console.WriteLine($"Regular SHA256 took {sw.ElapsedMilliseconds}ms to hash {n} messages ({sw.ElapsedMilliseconds / (double)n} ms per string)");
    }
}